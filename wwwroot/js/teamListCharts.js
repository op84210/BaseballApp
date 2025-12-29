// 這個檔案將用於球隊戰績折線圖的 ECharts 初始化與渲染
window.TeamListCharts = {
    renderWinRateLineChart: function (chartData) {
        const chartDom = document.getElementById('teamWinRateLineChart');
        if (!chartDom) return;
        const myChart = echarts.init(chartDom);
        const option = {
            title: {
                text: '各隊勝率與勝場變化',
                left: 'center',
                textStyle: { fontSize: 16 }
            },
            tooltip: {
                trigger: 'axis',
                axisPointer: { type: 'cross' }
            },
            legend: {
                top: 30
            },
            xAxis: {
                type: 'category',
                data: chartData.dates,
                axisLabel: { rotate: 45 }
            },
            yAxis: [
                {
                    type: 'value',
                    name: '勝場',
                    minInterval: 1
                },
                {
                    type: 'value',
                    name: '勝率',
                    min: 0,
                    max: 1,
                    axisLabel: {
                        formatter: function (val) { return (val * 100).toFixed(0) + '%'; }
                    }
                }
            ],
            series: chartData.teams.map(team => [
                {
                    name: team.name + ' 勝場',
                    type: 'line',
                    yAxisIndex: 0,
                    data: team.wins,
                    smooth: true
                },
                {
                    name: team.name + ' 勝率',
                    type: 'line',
                    yAxisIndex: 1,
                    data: team.winRates,
                    smooth: true
                }
            ]).flat()
        };
        myChart.setOption(option);
    }
};
