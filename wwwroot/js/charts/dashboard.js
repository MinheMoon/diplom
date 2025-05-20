const connection = new signalR.HubConnectionBuilder()
    .withUrl("/metricsHub")
    .configureLogging(signalR.LogLevel.Information)
    .build();

let cpuChartData = {
    labels: Array(20).fill(""),
    datasets: [{
        label: 'ЦП (%)',
        data: Array(20).fill(0),
        borderColor: 'rgba(255, 99, 132, 1)',
        backgroundColor: 'rgba(255, 99, 132, 0.2)',
        fill: true,
        tension: 0.4
    }]
};

let memoryChartData = {
    labels: Array(20).fill(""),
    datasets: [{
        label: "Пам'ять (%)",
        data: Array(20).fill(0),
        borderColor: 'rgba(75, 192, 192, 1)',
        backgroundColor: 'rgba(75, 192, 192, 0.2)',
        fill: true,
        tension: 0.4
    }]
};

const cpuChartConfig = {
    type: 'line',
    data: cpuChartData,
    options: {
        responsive: true,
        scales: {
            y: {
                beginAtZero: true,
                max: 100
            }
        },
        animation: {
            duration: 300
        }
    }
};

const memoryChartConfig = {
    type: 'line',
    data: memoryChartData,
    options: {
        responsive: true,
        scales: {
            y: {
                beginAtZero: true,
                max: 100
            }
        },
        animation: {
            duration: 300
        }
    }
};

let cpuChart;
let memoryChart;

function showAlerts(metrics) {
    const alertsContainer = document.getElementById("alerts-container");
    const alertsContent = document.getElementById("alerts-content");
    let hasAlerts = false;
    let alertsHtml = "";

    if (metrics.cpuUsagePercentage > 90) {
        hasAlerts = true;
        alertsHtml += `<p><strong>ЦП:</strong> ${metrics.cpuUsagePercentage.toFixed(1)}% (критичне значення)</p>`;
    }

    if (metrics.memoryUsagePercentage > 90) {
        hasAlerts = true;
        alertsHtml += `<p><strong>Пам'ять:</strong> ${metrics.memoryUsagePercentage.toFixed(1)}% (критичне значення)</p>`;
    }

    metrics.diskMetrics.forEach(disk => {
        const usagePercentage = (disk.usedSpaceGB / disk.totalSpaceGB * 100).toFixed(1);
        if (usagePercentage > 90) {
            hasAlerts = true;
            alertsHtml += `<p><strong>Диск ${disk.driveName}:</strong> ${usagePercentage}% (критичне значення)</p>`;
        }
    });

    metrics.networkMetrics.forEach(network => {
        let networkIssues = [];

        if (network.packetLoss > 20) {
            networkIssues.push(`Втрата пакетів: ${network.packetLoss.toFixed(1)}%`);
        }

        if (network.latency > 500) {
            networkIssues.push(`Висока затримка: ${network.latency.toFixed(1)} мс`);
        }

        if (!network.isConnected) {
            networkIssues.push("Відсутнє з'єднання");
        }

        if (network.bandwidthUsagePercentage > 90) {
            networkIssues.push(`Використання каналу: ${network.bandwidthUsagePercentage.toFixed(1)}%`);
        }

        if (networkIssues.length > 0) {
            hasAlerts = true;
            alertsHtml += `<p><strong>Мережа ${network.interfaceName}:</strong> ${networkIssues.join(", ")}</p>`;
        }
    });

    if (hasAlerts) {
        alertsContent.innerHTML = alertsHtml;
        alertsContainer.style.display = "block";
    } else {
        alertsContainer.style.display = "none";
    }
}

connection.on("ReceiveMetrics", function(metrics) {
    // Показ сповіщень про критичні показники
    showAlerts(metrics);

    document.getElementById("cpu-progress").style.width = `${metrics.cpuUsagePercentage.toFixed(1)}%`;
    document.getElementById("cpu-progress").textContent = `${metrics.cpuUsagePercentage.toFixed(1)}%`;
    document.getElementById("cpu-progress").setAttribute("aria-valuenow", metrics.cpuUsagePercentage.toFixed(1));

    if (metrics.cpuUsagePercentage > 90) {
        document.getElementById("cpu-progress").className = "progress-bar bg-danger";
    } else if (metrics.cpuUsagePercentage > 70) {
        document.getElementById("cpu-progress").className = "progress-bar bg-warning";
    } else {
        document.getElementById("cpu-progress").className = "progress-bar";
    }

    document.getElementById("memory-progress").style.width = `${metrics.memoryUsagePercentage.toFixed(1)}%`;
    document.getElementById("memory-progress").textContent = `${metrics.memoryUsagePercentage.toFixed(1)}%`;
    document.getElementById("memory-progress").setAttribute("aria-valuenow", metrics.memoryUsagePercentage.toFixed(1));

    document.getElementById("memory-details").textContent =
        `${metrics.usedMemoryGB.toFixed(2)} ГБ / ${metrics.totalMemoryGB.toFixed(2)} ГБ`;
    document.getElementById("memory-free").textContent =
        `Вільно: ${metrics.freeMemoryGB.toFixed(2)} ГБ`;

    if (metrics.memoryUsagePercentage > 90) {
        document.getElementById("memory-progress").className = "progress-bar bg-danger";
    } else if (metrics.memoryUsagePercentage > 70) {
        document.getElementById("memory-progress").className = "progress-bar bg-warning";
    } else {
        document.getElementById("memory-progress").className = "progress-bar bg-success";
    }

    const timeLabel = new Date(metrics.timestamp).toLocaleTimeString();

    cpuChartData.labels.push(timeLabel);
    cpuChartData.datasets[0].data.push(metrics.cpuUsagePercentage);
    if (cpuChartData.labels.length > 20) {
        cpuChartData.labels.shift();
        cpuChartData.datasets[0].data.shift();
    }

    memoryChartData.labels.push(timeLabel);
    memoryChartData.datasets[0].data.push(metrics.memoryUsagePercentage);
    if (memoryChartData.labels.length > 20) {
        memoryChartData.labels.shift();
        memoryChartData.datasets[0].data.shift();
    }

    cpuChart.update();
    memoryChart.update();

    const diskContainer = document.getElementById("disk-metrics-container");
    let diskHtml = '';

    metrics.diskMetrics.forEach(disk => {
        const usagePercentage = (disk.usedSpaceGB / disk.totalSpaceGB * 100).toFixed(1);
        let barClass = "progress-bar bg-info";

        if (usagePercentage > 90) {
            barClass = "progress-bar bg-danger";
        } else if (usagePercentage > 70) {
            barClass = "progress-bar bg-warning";
        }

        diskHtml += `
            <div class="mb-3">
                <div class="d-flex justify-content-between mb-1">
                    <span>${disk.driveName}</span>
                    <span>${disk.usedSpaceGB.toFixed(2)} ГБ / ${disk.totalSpaceGB.toFixed(2)} ГБ (${usagePercentage}%)</span>
                </div>
                <div class="progress">
                    <div class="${barClass}" role="progressbar" style="width: ${usagePercentage}%;" 
                         aria-valuenow="${usagePercentage}" aria-valuemin="0" aria-valuemax="100">${usagePercentage}%</div>
                </div>
                <small class="text-muted">Вільно: ${disk.freeSpaceGB.toFixed(2)} ГБ</small>
            </div>
        `;
    });

    diskContainer.innerHTML = diskHtml;

    const networkContainer = document.getElementById("network-metrics-container");
    let networkHtml = `<div class="table-responsive">
                        <table class="table table-striped">
                        <thead>
                            <tr>
                                <th>Інтерфейс</th>
                                <th>Статус</th>
                                <th>Відправлено (КБ/с)</th>
                                <th>Отримано (КБ/с)</th>
                                <th>Затримка</th>
                                <th>Втрата пакетів</th>
                                <th>Використання каналу</th>
                            </tr>
                        </thead>
                        <tbody>`;

    metrics.networkMetrics.forEach(network => {
        let statusClass = network.isConnected ? "text-success" : "text-danger";
        let statusText = network.isConnected ? "Підключено" : "Відключено";

        let latencyClass = "text-success";
        if (network.latency > 500) {
            latencyClass = "text-danger";
        } else if (network.latency > 200) {
            latencyClass = "text-warning";
        }

        let packetLossClass = "text-success";
        if (network.packetLoss > 20) {
            packetLossClass = "text-danger";
        } else if (network.packetLoss > 5) {
            packetLossClass = "text-warning";
        }

        let bandwidthClass = "text-success";
        if (network.bandwidthUsagePercentage > 90) {
            bandwidthClass = "text-danger";
        } else if (network.bandwidthUsagePercentage > 70) {
            bandwidthClass = "text-warning";
        }

        networkHtml += `
            <tr>
                <td>${network.interfaceName}</td>
                <td><span class="${statusClass}">${statusText}</span></td>
                <td>${(network.bytesSent / 1024).toFixed(2)}</td>
                <td>${(network.bytesReceived / 1024).toFixed(2)}</td>
                <td><span class="${latencyClass}">${network.latency.toFixed(1)} мс</span></td>
                <td><span class="${packetLossClass}">${network.packetLoss.toFixed(1)}%</span></td>
                <td><span class="${bandwidthClass}">${network.bandwidthUsagePercentage.toFixed(1)}%</span></td>
            </tr>
        `;
    });

    networkHtml += '</tbody></table></div>';
    networkContainer.innerHTML = networkHtml;
});

function requestMetrics() {
    if (connection.state === signalR.HubConnectionState.Connected) {
        connection.invoke("RequestCurrentMetrics").catch(function(err) {
            console.error(err);
        });
    }
}

async function startConnection() {
    try {
        await connection.start();
        console.log("SignalR Connected.");

        cpuChart = new Chart(document.getElementById('cpuChart'), cpuChartConfig);
        memoryChart = new Chart(document.getElementById('memoryChart'), memoryChartConfig);

        requestMetrics();
        setInterval(requestMetrics, 2000);
    } catch (err) {
        console.error(err);
        setTimeout(startConnection, 5000);
    }
}

connection.onclose(async () => {
    await startConnection();
});

document.addEventListener('DOMContentLoaded', startConnection);