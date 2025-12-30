// 這個檔案將用於球隊戰績折線圖的 ECharts 初始化與渲染
(function () {
    'use strict';

    let winRateChart = null;
    let TeamsWinRate = {};

    function init(seasonId) {
        // 初始化圖表容器
        const teamWinRateLineChart = document.getElementById('teamWinRateLineChart');

        if (teamWinRateLineChart) {
            winRateChart = echarts.init(teamWinRateLineChart);
        }

        // 從 API 載入數據並渲染圖表
        if (seasonId) {
            loadChartData(seasonId);
        }

        // 設定視窗大小調整事件
        window.addEventListener('resize', function () {
            if (teamWinRateLineChart) teamWinRateLineChart.resize();
        });
    }

    // 從 API 載入圖表數據
    async function loadChartData(seasonId) {
        try {
            const [winRateLineResponse] = await Promise.all([
                fetch(`/api/data/winRate/chart?seasonId=${seasonId}`)
            ]);

            if (winRateLineResponse.ok) {
                const winRateData = await winRateLineResponse.json();
                if (winRateData.hasData !== false) {
                    TeamsWinRate = winRateData.chartData || {};
                }
            }

            // 渲染所有圖表
            renderWinRateLineChart();

        } catch (error) {
            console.error('載入圖表數據失敗:', error);
        }
    }

    // 渲染勝率折線圖
    function renderWinRateLineChart() {

        // 確認圖表已初始化
        if (!winRateChart)
            return;

        // 檢查是否有有效數據
        if (!TeamsWinRate || !TeamsWinRate.dates || TeamsWinRate.dates.length === 0) {
            winRateChart.setOption({
                title: {
                    text: '無法顯示折線圖',
                    left: 'center',
                    textStyle: { fontSize: 16, color: '#999' },
                    subtext: '可能尚無比賽數據可供顯示',
                    subtextStyle: { fontSize: 12, color: '#999' }
                },
                graphic: {
                    type: 'text',
                    left: 'center',
                    top: 'middle',
                    style: {
                        text: '⚠ 沒有比賽數據',
                        fontSize: 16,
                        fill: '#999',
                        textAlign: 'center'
                    }
                }
            });
            return;
        }

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
                data: TeamsWinRate.dates,
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
            series: TeamsWinRate.teams.map(team => [
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

    window.TeamListCharts = {
        init: init,
        renderWinRateLineChart: renderWinRateLineChart
    };
})();
