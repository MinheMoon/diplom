const connection = new signalR.HubConnectionBuilder()
    .withUrl("/metricsHub")
    .configureLogging(signalR.LogLevel.Information)
    .build();

let reportChart;

connection.on("ReceiveHistoricalMetrics", function(metrics) {
    const reportContainer = document.getElementById("report-container");
    const reportChartCanvas = document.getElementById("reportChart");

    if (metrics.length === 0) {
        reportContainer.innerHTML = '<div class="alert alert-warning">Немає даних для обраного періоду</div>';
        reportChartCanvas.style.display = 'none';
        return;
    }

    const labels = metrics.map(m => new Date(m.timestamp).toLocaleTimeString());
    const cpuData = metrics.map(m => m.cpuUsagePercentage);
    const memoryData = metrics.map(m => m.memoryUsagePercentage);

    const avgCpu = cpuData.reduce((a, b) => a + b, 0) / cpuData.length;
    const avgMemory = memoryData.reduce((a, b) => a + b, 0) / memoryData.length;
    const maxCpu = Math.max(...cpuData);
    const maxMemory = Math.max(...memoryData);

    reportContainer.innerHTML = `
        <div class="row">
            <div class="col-md-6">
                <div class="card">
                    <div class="card-header">Статистика ЦП</div>
                    <div class="card-body">
                        <p>Середнє використання: ${avgCpu.toFixed(2)}%</p>
                        <p>Максимальне використання: ${maxCpu.toFixed(2)}%</p>
                    </div>
                </div>
            </div>
            <div class="col-md-6">
                <div class="card">
                    <div class="card-header">Статистика пам'яті</div>
                    <div class="card-body">
                        <p>Середнє використання: ${avgMemory.toFixed(2)}%</p>
                        <p>Максимальне використання: ${maxMemory.toFixed(2)}%</p>
                    </div>
                </div>
            </div>
        </div>
    `;

    reportChartCanvas.style.display = 'block';

    const chartData = {
        labels: labels,
        datasets: [
            {
                label: 'ЦП (%)',
                data: cpuData,
                borderColor: 'rgba(255, 99, 132, 1)',
                backgroundColor: 'rgba(255, 99, 132, 0.2)',
                fill: true,
                tension: 0.4
            },
            {
                label: "Пам'ять (%)",
                data: memoryData,
                borderColor: 'rgba(75, 192, 192, 1)',
                backgroundColor: 'rgba(75, 192, 192, 0.2)',
                fill: true,
                tension: 0.4
            }
        ]
    };

    if (reportChart) {
        reportChart.destroy();
    }

    reportChart = new Chart(reportChartCanvas, {
        type: 'line',
        data: chartData,
        options: {
            responsive: true,
            scales: {
                y: {
                    beginAtZero: true,
                    max: 100
                }
            }
        }
    });
});

document.addEventListener('DOMContentLoaded', function() {
    const reportForm = document.getElementById('report-form');

    reportForm.addEventListener('submit', function(e) {
        e.preventDefault();

        const startDate = new Date(document.getElementById('start-date').value);
        const endDate = new Date(document.getElementById('end-date').value);

        if (startDate >= endDate) {
            alert('Початкова дата повинна бути меншою за кінцеву');
            return;
        }

        if (connection.state === signalR.HubConnectionState.Connected) {
            connection.invoke("RequestHistoricalMetrics", startDate, endDate)
                .catch(function(err) {
                    console.error(err);
                });
        }
    });

    const endDate = new Date();
    const startDate = new Date();
    startDate.setHours(startDate.getHours() - 1); 

    document.getElementById('start-date').value = startDate.toISOString().slice(0, 16);
    document.getElementById('end-date').value = endDate.toISOString().slice(0, 16);
});

async function startConnection() {
    try {
        await connection.start();
        console.log("SignalR Connected.");
    } catch (err) {
        console.error(err);
        setTimeout(startConnection, 5000);
    }
}

connection.onclose(async () => {
    await startConnection();
});

startConnection();