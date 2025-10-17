//等待頁面加載
document.addEventListener("DOMContentLoaded", function() {
    // 初始化圖表
    initCharts();
});

function initCharts() {

    // 打擊率趨勢圖
    var battingChart = echarts.init(document.getElementById('battingChart'));
    var battingOption = {
        title: {
            text: '球員打擊率走勢'
        },
        tooltip: {
            trigger: 'axis',
            formatter: function (params) {
                var result = params[0].name + '<br/>';
                params.forEach(function (item) {
                    if (!item.value) {
                        result += '受傷缺賽<br/>';
                    } else {
                        result += '打擊率: ' + item.value + '<br/>';
                    }
                });
                return result;
            }
        },
        legend: {
            data: ['張育成']
        },
        xAxis: {
            type: 'category',
            data: ['1月', '2月', '3月', '4月', '5月', '6月']
        },
        yAxis: {
            type: 'value'
        },
        series: [
            {
                name: '張育成',
                type: 'line',
                data: [0.285, 0.298, 0.312, 0.312, 0.289, 0.301], // 3月受傷
                smooth: true
            }
        ]
    };
    battingChart.setOption(battingOption);

    // 全壘打統計圖
    var homerunChart = echarts.init(document.getElementById('homerunChart'));
    var homerunOption = {
        title: {
            text: '全壘打統計'
        },
        tooltip: {
            trigger: 'axis',
            axisPointer: {
                type: 'shadow'
            },
            formatter: function (params) {
                var result = params[0].name + '<br/>';
                params.forEach(function (item) {
                    var status = '';
                    if (item.value === 0) {
                        status = ' (該月無全壘打)';
                    }
                    result += item.marker + item.seriesName + ': ' + item.value + status + '<br/>';
                });
                return result;
            }
        },
        xAxis: {
            type: 'category',
            data: ['1月', '2月', '3月', '4月', '5月']
        },
        yAxis: {
            type: 'value',
            name: '全壘打數'
        },
        series: [
            {
                name: '全壘打',
                type: 'bar',
                data: [1, 5, 4, 3, 2]
            }
        ]
    };
    homerunChart.setOption(homerunOption);

    // 球員表現雷達圖
    var radarChart = echarts.init(document.getElementById('radarChart'));
    var radarOption = {
        title: {
            text: '張育成綜合表現'
        },
        tooltip: {
            formatter: function (params) {
                var result = params.name + '<br/>';
                params.value.forEach(function (val, index) {
                    var indicators = ['打擊率', '上壘率', '長打率', 'OPS', '盜壘成功率'];
                    var status = val === 0 ? ' (數據不足)' : '';
                    result += indicators[index] + ': ' + val + status + '<br/>';
                });
                return result;
            }
        },
        legend: {
            data: ['2024年', '2023年', '2024年(受傷調整)'],
            bottom: 10
        },
        radar: {
            indicator: [
                { name: '打擊率', max: 0.35 },
                { name: '上壘率', max: 0.4 },
                { name: '長打率', max: 0.6 },
                { name: 'OPS', max: 1.0 },
                { name: '盜壘成功率', max: 1.0 }
            ]
        },
        series: [
            {
                name: '球員表現',
                type: 'radar',
                data: [
                    {
                        value: [0.301, 0.356, 0.487, 0.843, 0.75],
                        name: '2024年',
                        itemStyle: { color: '#5470c6' },
                        areaStyle: { opacity: 0.3 }
                    },
                    {
                        value: [0.289, 0.334, 0.456, 0.79, 0.82],
                        name: '2023年',
                        itemStyle: { color: '#91cc75' },
                        areaStyle: { opacity: 0.3 }
                    },
                    {
                        value: [0.301, 0.356, 0, 0.843, 0.75], // 長打率數據因受傷不足
                        name: '2024年(受傷調整)',
                        itemStyle: { color: '#fac858' },
                        lineStyle: { type: 'dashed' },
                        areaStyle: { opacity: 0.1 }
                    }
                ]
            }
        ]
    };
    radarChart.setOption(radarOption);

    // 響應式調整
    window.addEventListener('resize', function () {
        battingChart.resize();
        homerunChart.resize();
        radarChart.resize();
    });
}