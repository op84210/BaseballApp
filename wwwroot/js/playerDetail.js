// PlayerDetail 頁面的圖表與表格管理
(function () {
    'use strict';
    
    // 全域變數
    let batterLineChart = null;
    let batterRadarChart = null;
    let pitcherLineChart = null;
    let pitcherRadarChart = null;
    let pitchTypeChart = null;

    // 打者數據變數
    let batterGames = [];
    let batterPR = {};
    let batterMedianStats = {};
    let batterTeamAvg = {};
    let batterTeamPR = {};
    let batterRadarStats = {};

    // 投手數據變數
    let pitcherGames = [];
    let pitcherPR = {};
    let pitcherMedianStats = {};
    let pitcherTeamAvg = {};
    let pitcherTeamPR = {};
    let pitcherRadarStats = {};

    // 初始化函數（新版：支援 API 載入）
    function init(playerId, seasonId) {
        // 初始化圖表容器
        const batterLineChartElement = document.getElementById('batterLineChart');
        const batterRadarChartElement = document.getElementById('batterRadarChart');
        const pitcherLineChartElement = document.getElementById('pitcherLineChart');
        const pitcherRadarChartElement = document.getElementById('pitcherRadarChart');
        const pitchTypeChartElement = document.getElementById('pitchTypeChart');

        if (batterLineChartElement) {
            batterLineChart = echarts.init(batterLineChartElement);
        }
        if (batterRadarChartElement) {
            batterRadarChart = echarts.init(batterRadarChartElement);
        }
        if (pitcherLineChartElement) {
            pitcherLineChart = echarts.init(pitcherLineChartElement);
        }
        if (pitcherRadarChartElement) {
            pitcherRadarChart = echarts.init(pitcherRadarChartElement);
        }
        if (pitchTypeChartElement) {
            pitchTypeChart = echarts.init(pitchTypeChartElement);
        }

        // 從 API 載入數據並渲染圖表
        if (playerId && seasonId) {
            loadChartData(playerId, seasonId);
        }

        // 設定視窗大小調整事件
        window.addEventListener('resize', function() {
            if (batterLineChart) batterLineChart.resize();
            if (batterRadarChart) batterRadarChart.resize();
            if (pitcherLineChart) pitcherLineChart.resize();
            if (pitcherRadarChart) pitcherRadarChart.resize();
        });

        // 初始化 DataTable
        initDataTable();
    }

    // 從 API 載入圖表數據
    async function loadChartData(playerId, seasonId) {
        try {
            // 雙棲球員：同時載入投打數據
            const [batterResponse, pitcherResponse] = await Promise.all([
                fetch(`/api/playerdata/batter/${playerId}/chart?seasonId=${seasonId}`),
                fetch(`/api/playerdata/pitcher/${playerId}/chart?seasonId=${seasonId}`)
            ]);

            let hasBatterData = false;
            let hasPitcherData = false;

            if (batterResponse.ok) {
                const batterData = await batterResponse.json();
                if (batterData.hasData !== false) {
                    batterGames = batterData.chartData || [];
                    batterPR = batterData.percentileRanks || {};
                    batterMedianStats = batterData.leagueMedianStats || {};
                    batterTeamAvg = batterData.teamAverages || {};
                    batterTeamPR = batterData.teamPercentileRanks || {};
                    batterRadarStats = batterData.radarStats || {};
                    hasBatterData = true;
                }
            }

            if (pitcherResponse.ok) {
                const pitcherData = await pitcherResponse.json();
                if (pitcherData.hasData !== false) {
                    pitcherGames = pitcherData.chartData || [];
                    pitcherPR = pitcherData.percentileRanks || {};
                    pitcherMedianStats = pitcherData.leagueMedianStats || {};
                    pitcherTeamAvg = pitcherData.teamAverages || {};
                    pitcherTeamPR = pitcherData.teamPercentileRanks || {};
                    pitcherRadarStats = pitcherData.radarStats || {};
                    hasPitcherData = true;
                }
            }

            // 渲染所有圖表
            renderCharts(seasonId);
        
        } catch (error) {
            console.error('載入圖表數據失敗:', error);
        }
    }

    // 渲染折線圖
    function renderBatterLineChart(seasonId) {
        if (!seasonId || !batterLineChart) {
            return;
        }

        if (!batterGames.length) {
            batterLineChart.setOption({
                title: { 
                    text: '無法顯示折線圖', 
                    left: 'center', 
                    textStyle: { fontSize: 16, color: '#999' },
                    subtext: '該球員在此賽季沒有打擊數據',
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

        // 準備數據
        const cumulativeAVGData = batterGames.map(g => +g.avgData.toFixed(3));
        const cumulativeOPSData = batterGames.map(g => +g.opsData.toFixed(3));
        const gameAVGData = batterGames.map(g => g.gameAVG ? +g.gameAVG.toFixed(3) : 0);
        const gameOPSData = batterGames.map(g => g.gameOPS ? +g.gameOPS.toFixed(3) : 0);
        const dates = batterGames.map(g => new Date(g.date).toLocaleDateString('zh-TW', { month: 'numeric', day: 'numeric' }));

        // 聯盟平均值
        const leagueAVG = batterMedianStats.AVG ? +batterMedianStats.AVG.toFixed(3) : null;
        const leagueOPS = batterMedianStats.OPS ? +batterMedianStats.OPS.toFixed(3) : null;

        // 混合圖表配置：柱狀圖(單場) + 折線圖(累積)
        const option = {
            title: { 
                text: '打擊表現趨勢 (單場 & 累積)', 
                left: 'center', 
                textStyle: { fontSize: 14 }
            },
            tooltip: {
                trigger: 'axis',
                axisPointer: {
                    type: 'cross',
                    crossStyle: {
                        color: '#999'
                    }
                },
                formatter: function(params) {
                    let result = `<strong>${params[0].axisValue}</strong><br/>`;
                    params.forEach(item => {
                        if (item.seriesName.includes('聯盟')) {
                            // 跳過聯盟平均線的tooltip
                            return;
                        }
                        result += `${item.marker}${item.seriesName}: ${item.value}<br/>`;
                    });
                    if (leagueAVG) {
                        result += `<span style="display:inline-block;width:10px;height:10px;border-radius:5px;background-color:#91cc75;margin-right:5px;"></span>聯盟平均AVG: ${leagueAVG.toFixed(3)}<br/>`;
                    }
                    if (leagueOPS) {
                        result += `<span style="display:inline-block;width:10px;height:10px;border-radius:5px;background-color:#fac858;margin-right:5px;"></span>聯盟平均OPS: ${leagueOPS.toFixed(3)}<br/>`;
                    }
                    return result;
                }
            },
            legend: { 
                top: 25,
                data: ['單場AVG', '單場OPS', '累積AVG', '累積OPS', '聯盟平均AVG', '聯盟平均OPS']
            },
            grid: { 
                left: 65, 
                right: 65, 
                top: 70, 
                bottom: 40 
            },
            xAxis: {
                type: 'category',
                data: dates,
                axisPointer: {
                    type: 'shadow'
                }
            },
            yAxis: [
                {
                    type: 'value',
                    name: 'AVG',
                    position: 'left',
                    axisLabel: {
                        formatter: '{value}'
                    },
                    splitLine: {
                        lineStyle: {
                            type: 'dashed',
                            color: '#e0e0e0'
                        }
                    }
                },
                {
                    type: 'value',
                    name: 'OPS',
                    position: 'right',
                    axisLabel: {
                        formatter: '{value}'
                    },
                    splitLine: {
                        show: false
                    }
                }
            ],
            series: [
                // 單場AVG - 柱狀圖
                {
                    name: '單場AVG',
                    type: 'bar',
                    yAxisIndex: 0,
                    data: gameAVGData,
                    itemStyle: { 
                        color: '#91cc75',
                        opacity: 0.6
                    },
                    barMaxWidth: 20,
                    emphasis: {
                        itemStyle: {
                            opacity: 0.9
                        }
                    }
                },
                // 單場OPS - 柱狀圖
                {
                    name: '單場OPS',
                    type: 'bar',
                    yAxisIndex: 1,
                    data: gameOPSData,
                    itemStyle: { 
                        color: '#fac858',
                        opacity: 0.6
                    },
                    barMaxWidth: 20,
                    emphasis: {
                        itemStyle: {
                            opacity: 0.9
                        }
                    }
                },
                // 累積AVG - 折線圖
                {
                    name: '累積AVG',
                    type: 'line',
                    yAxisIndex: 0,
                    smooth: true,
                    data: cumulativeAVGData,
                    itemStyle: { color: '#5470c6' },
                    lineStyle: { width: 3 },
                    symbolSize: 6,
                    emphasis: {
                        focus: 'series'
                    },
                    z: 10
                },
                // 累積OPS - 折線圖
                {
                    name: '累積OPS',
                    type: 'line',
                    yAxisIndex: 1,
                    smooth: true,
                    data: cumulativeOPSData,
                    itemStyle: { color: '#ee6666' },
                    lineStyle: { width: 3 },
                    symbolSize: 6,
                    emphasis: {
                        focus: 'series'
                    },
                    z: 10
                }
            ]
        };

        // 加入聯盟平均參考線
        if (leagueAVG) {
            option.series.push({
                name: '聯盟平均AVG',
                type: 'line',
                yAxisIndex: 0,
                data: Array(dates.length).fill(leagueAVG),
                lineStyle: {
                    type: 'dashed',
                    width: 1.5,
                    color: '#91cc75'
                },
                itemStyle: { 
                    color: '#91cc75',
                    opacity: 0
                },
                symbol: 'none',
                emphasis: {
                    disabled: true
                },
                z: 1
            });
        }

        if (leagueOPS) {
            option.series.push({
                name: '聯盟平均OPS',
                type: 'line',
                yAxisIndex: 1,
                data: Array(dates.length).fill(leagueOPS),
                lineStyle: {
                    type: 'dashed',
                    width: 1.5,
                    color: '#fac858'
                },
                itemStyle: { 
                    color: '#fac858',
                    opacity: 0
                },
                symbol: 'none',
                emphasis: {
                    disabled: true
                },
                z: 1
            });
        }

        batterLineChart.setOption(option);
    }

    // 渲染雷達圖
    function renderBatterRadarChart(seasonId) {
        if (!seasonId || !batterRadarChart) {
            return;
        }

        // 檢查是否有數據
        if (!batterGames.length) {
            batterRadarChart.setOption({
                title: { 
                    text: '無法顯示雷達圖', 
                    left: 'center', 
                    textStyle: { fontSize: 16, color: '#999' },
                    subtext: '該球員在此賽季沒有打擊數據',
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

        // 雷達圖配置 - 顯示 PR值
        if (batterPR && Object.keys(batterPR).length > 0) {

            batterRadarChart.setOption({
                title: { 
                    text: '能力PR值雷達圖', 
                    left: 'center', 
                    textStyle: { fontSize: 14 },
                    subtextStyle: { fontSize: 11, color: '#999' }
                },
                tooltip: {
                    trigger: 'item',
                    formatter: function(params) {
                        const statNames = [
                            { en: 'AVG', zh: '打擊率', key: 'avg' },
                            { en: 'OBP', zh: '上壘率', key: 'obp' },
                            { en: 'SLG', zh: '長打率', key: 'slg' },
                            { en: 'OPS', zh: 'OPS', key: 'ops' },
                            { en: 'RBI', zh: '打點', key: 'rbi' },
                            { en: 'SO', zh: '三振', key: 'so' },
                            { en: 'BB', zh: '保送', key: 'bb' },
                            { en: 'R', zh: '得分', key: 'r' }
                        ];

                        if (params.name === '球員PR值') {
                            let result = '<strong>球員能力 PR值</strong><br/>';
                            statNames.forEach((stat) => {
                                const prValue = batterPR[stat.en] || 0;
                                const actualValue = batterRadarStats[stat.key] || 0;
                                let formatValue;
                                if (stat.en === 'SO' || stat.en === 'BB') {
                                    formatValue = actualValue.toFixed(1) + '%';
                                } else if (stat.en === 'RBI') {
                                    formatValue = actualValue.toFixed(0);
                                } else {
                                    formatValue = actualValue.toFixed(3);
                                }
                                if (stat.en === 'R') {
                                    formatValue = actualValue.toFixed(1) + '%';
                                }
                                result += `${stat.zh}(${stat.en}): ${formatValue} | PR${prValue.toFixed(1)}<br/>`;
                            });
                            return result;
                        } else if (params.name === '隊伍平均') {
                            let result = '<strong>隊伍平均</strong><br/>';
                            statNames.forEach((stat) => {
                                const avgValue = batterTeamAvg[stat.en] || 0;
                                const prValue = batterTeamPR?.[stat.en] || 50;
                                let formatValue;
                                if (stat.en === 'SO' || stat.en === 'BB') {
                                    formatValue = avgValue.toFixed(1) + '%';
                                } else if (stat.en === 'RBI') {
                                    formatValue = avgValue.toFixed(0);
                                } else {
                                    formatValue = avgValue.toFixed(3);
                                }
                                if (stat.en === 'R') {
                                    formatValue = avgValue.toFixed(1) + '%';
                                }
                                result += `${stat.zh}(${stat.en}): ${formatValue} | PR${prValue.toFixed(1)}<br/>`;
                            });
                            return result;
                        } else {
                            let result = '<strong>聯盟中位數</strong><br/>';
                            statNames.forEach((stat) => {
                                const avgValue = batterMedianStats[stat.en] || 0;
                                let formatValue;
                                if (stat.en === 'SO' || stat.en === 'BB') {
                                    formatValue = avgValue.toFixed(1) + '%';
                                } else if (stat.en === 'RBI') {
                                    formatValue = avgValue.toFixed(0);
                                } else {
                                    formatValue = avgValue.toFixed(3);
                                }
                                if (stat.en === 'R') {
                                    formatValue = avgValue.toFixed(1) + '%';
                                }
                                result += `${stat.zh}(${stat.en}): ${formatValue}<br/>`;
                            });
                            return result;
                        }
                    }
                },
                legend: { 
                    top: 25,
                    data: ['球員PR值', '隊伍平均', '聯盟中位數']
                },
                radar: {
                    indicator: [
                        { name: 'AVG', max: 100 },
                        { name: 'OBP', max: 100 },
                        { name: 'SLG', max: 100 },
                        { name: 'OPS', max: 100 },
                        { name: 'RBI', max: 100 },
                        { name: 'SO(少為佳)', max: 100 },
                        { name: 'BB', max: 100 },
                        { name: 'R', max: 100 }
                    ],
                    center: ['50%', '58%'],
                    radius: '60%'
                },
                series: [{
                    type: 'radar',
                    data: [
                        {
                            value: [
                                batterPR.AVG || 0,
                                batterPR.OBP || 0,
                                batterPR.SLG || 0,
                                batterPR.OPS || 0,
                                batterPR.RBI || 0,
                                batterPR.SO || 0,
                                batterPR.BB || 0,
                                batterPR.R || 0
                            ],
                            name: '球員PR值',
                            areaStyle: { opacity: 0.3 },
                            lineStyle: { width: 2 },
                            itemStyle: { color: '#5470c6' }
                        },
                        {
                            value: [
                                batterTeamPR?.AVG || 50,
                                batterTeamPR?.OBP || 50,
                                batterTeamPR?.SLG || 50,
                                batterTeamPR?.OPS || 50,
                                batterTeamPR?.RBI || 50,
                                batterTeamPR?.SO || 50,
                                batterTeamPR?.BB || 50,
                                batterTeamPR?.R || 50
                            ],
                            name: '隊伍平均',
                            lineStyle: { 
                                type: 'solid',
                                width: 1.5,
                                color: '#fac858'
                            },
                            itemStyle: { color: '#fac858' },
                            areaStyle: { opacity: 0 }
                        },
                        {
                            value:  [50, 50, 50, 50, 50, 50, 50, 50],  // 聯盟中位數 (PR50)
                            name: '聯盟中位數',
                            lineStyle: { 
                                type: 'dashed',
                                width: 1.5,
                                color: '#91cc75'
                            },
                            itemStyle: { color: '#91cc75' },
                            areaStyle: { opacity: 0 }
                        }
                    ]
                }]
            });
        } else {
            // 沒有PR值資料時顯示錯誤提示
            batterRadarChart.setOption({
                title: { 
                    text: '無法顯示雷達圖', 
                    left: 'center', 
                    textStyle: { fontSize: 16, color: '#999' },
                    subtext: '缺少 PR 值數據',
                    subtextStyle: { fontSize: 12, color: '#999' }
                },
                graphic: {
                    type: 'text',
                    left: 'center',
                    top: 'middle',
                    style: {
                        text: '⚠ 數據不足\n無法計算百分位排名',
                        fontSize: 16,
                        fill: '#999',
                        textAlign: 'center'
                    }
                }
            });
        }
    }

    // 渲染圖表 (呼叫折線圖和雷達圖)
    function renderCharts(seasonId) {
        renderBatterLineChart(seasonId);
        renderBatterRadarChart(seasonId);
        renderPitcherLineChart(seasonId);
        renderPitcherRadarChart(seasonId);
        renderPitchTypeChart(seasonId);
    }

    // 渲染投手折線圖
    function renderPitcherLineChart(seasonId) {
        if (!seasonId || !pitcherLineChart) {
            return;
        }

        if (!pitcherGames.length) {
            pitcherLineChart.setOption({
                title: { 
                    text: '無法顯示折線圖', 
                    left: 'center', 
                    textStyle: { fontSize: 16, color: '#999' },
                    subtext: '該球員在此賽季沒有投球數據',
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

        // 準備數據
        const cumulativeERAData = pitcherGames.map(g => +g.era.toFixed(2));
        const cumulativeWHIPData = pitcherGames.map(g => +g.whip.toFixed(2));
        const gameERAData = pitcherGames.map(g => g.gameERA ? +g.gameERA.toFixed(2) : 0);
        const gameWHIPData = pitcherGames.map(g => g.gameWHIP ? +g.gameWHIP.toFixed(2) : 0);
        const dates = pitcherGames.map(g => new Date(g.date).toLocaleDateString('zh-TW', { month: 'numeric', day: 'numeric' }));

        // 聯盟平均值
        const leagueERA = pitcherMedianStats.ERA ? +pitcherMedianStats.ERA.toFixed(2) : null;
        const leagueWHIP = pitcherMedianStats.WHIP ? +pitcherMedianStats.WHIP.toFixed(2) : null;

        // 混合圖表配置：柱狀圖(單場) + 折線圖(累積)
        const option = {
            title: { 
                text: '投球表現趨勢 (單場 & 累積)', 
                left: 'center', 
                textStyle: { fontSize: 14 }
            },
            tooltip: {
                trigger: 'axis',
                axisPointer: {
                    type: 'cross',
                    crossStyle: {
                        color: '#999'
                    }
                },
                formatter: function(params) {
                    let result = `<strong>${params[0].axisValue}</strong><br/>`;
                    params.forEach(item => {
                        if (item.seriesName.includes('聯盟')) {
                            // 跳過聯盟平均線的tooltip
                            return;
                        }
                        result += `${item.marker}${item.seriesName}: ${item.value}<br/>`;
                    });
                    if (leagueERA) {
                        result += `<span style="display:inline-block;width:10px;height:10px;border-radius:5px;background-color:#91cc75;margin-right:5px;"></span>聯盟平均ERA: ${leagueERA}<br/>`;
                    }
                    if (leagueWHIP) {
                        result += `<span style="display:inline-block;width:10px;height:10px;border-radius:5px;background-color:#fac858;margin-right:5px;"></span>聯盟平均WHIP: ${leagueWHIP}<br/>`;
                    }
                    return result;
                }
            },
            legend: { 
                top: 25,
                data: ['單場ERA', '單場WHIP', '累積ERA', '累積WHIP', '聯盟平均ERA', '聯盟平均WHIP']
            },
            grid: { 
                left: 65, 
                right: 65, 
                top: 70, 
                bottom: 40 
            },
            xAxis: {
                type: 'category',
                data: dates,
                axisPointer: {
                    type: 'shadow'
                }
            },
            yAxis: [
                {
                    type: 'value',
                    name: 'ERA',
                    position: 'left',
                    axisLabel: {
                        formatter: '{value}'
                    },
                    splitLine: {
                        lineStyle: {
                            type: 'dashed',
                            color: '#e0e0e0'
                        }
                    }
                },
                {
                    type: 'value',
                    name: 'WHIP',
                    position: 'right',
                    axisLabel: {
                        formatter: '{value}'
                    },
                    splitLine: {
                        show: false
                    }
                }
            ],
            series: [
                // 單場ERA - 柱狀圖
                {
                    name: '單場ERA',
                    type: 'bar',
                    yAxisIndex: 0,
                    data: gameERAData,
                    itemStyle: { 
                        color: '#91cc75',
                        opacity: 0.6
                    },
                    barMaxWidth: 20,
                    emphasis: {
                        itemStyle: {
                            opacity: 0.9
                        }
                    }
                },
                // 單場WHIP - 柱狀圖
                {
                    name: '單場WHIP',
                    type: 'bar',
                    yAxisIndex: 1,
                    data: gameWHIPData,
                    itemStyle: { 
                        color: '#fac858',
                        opacity: 0.6
                    },
                    barMaxWidth: 20,
                    emphasis: {
                        itemStyle: {
                            opacity: 0.9
                        }
                    }
                },
                // 累積ERA - 折線圖
                {
                    name: '累積ERA',
                    type: 'line',
                    yAxisIndex: 0,
                    smooth: true,
                    data: cumulativeERAData,
                    itemStyle: { color: '#5470c6' },
                    lineStyle: { width: 3 },
                    symbolSize: 6,
                    emphasis: {
                        focus: 'series'
                    },
                    z: 10
                },
                // 累積WHIP - 折線圖
                {
                    name: '累積WHIP',
                    type: 'line',
                    yAxisIndex: 1,
                    smooth: true,
                    data: cumulativeWHIPData,
                    itemStyle: { color: '#ee6666' },
                    lineStyle: { width: 3 },
                    symbolSize: 6,
                    emphasis: {
                        focus: 'series'
                    },
                    z: 10
                }
            ]
        };

        // 加入聯盟平均參考線
        if (leagueERA) {
            option.series.push({
                name: '聯盟平均ERA',
                type: 'line',
                yAxisIndex: 0,
                data: Array(dates.length).fill(leagueERA),
                lineStyle: {
                    type: 'dashed',
                    width: 1.5,
                    color: '#91cc75'
                },
                itemStyle: { 
                    color: '#91cc75',
                    opacity: 0
                },
                symbol: 'none',
                emphasis: {
                    disabled: true
                },
                z: 1
            });
        }

        if (leagueWHIP) {
            option.series.push({
                name: '聯盟平均WHIP',
                type: 'line',
                yAxisIndex: 1,
                data: Array(dates.length).fill(leagueWHIP),
                lineStyle: {
                    type: 'dashed',
                    width: 1.5,
                    color: '#fac858'
                },
                itemStyle: { 
                    color: '#fac858',
                    opacity: 0
                },
                symbol: 'none',
                emphasis: {
                    disabled: true
                },
                z: 1
            });
        }

        pitcherLineChart.setOption(option);
    }

    // 渲染投手雷達圖
    function renderPitcherRadarChart(seasonId) {
        if (!seasonId || !pitcherRadarChart) {
            return;
        }

        // 檢查是否有數據
        if (!pitcherGames.length) {
            pitcherRadarChart.setOption({
                title: { 
                    text: '無法顯示雷達圖', 
                    left: 'center', 
                    textStyle: { fontSize: 16, color: '#999' },
                    subtext: '該球員在此賽季沒有投球數據',
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

        // 雷達圖配置 - 顯示 PR值
        if (pitcherPR && Object.keys(pitcherPR).length > 0) {
            
            pitcherRadarChart.setOption({
                title: { 
                    text: '能力PR值雷達圖', 
                    left: 'center', 
                    textStyle: { fontSize: 14 },
                    subtextStyle: { fontSize: 11, color: '#999' }
                },
                tooltip: {
                    trigger: 'item',
                    formatter: function(params) {
                        const statNames = [
                            { en: 'ERA', zh: '防禦率', key: 'era' },
                            { en: 'WHIP', zh: 'WHIP', key: 'whip' },
                            { en: 'K/9', zh: '每九局三振率', key: 'k9' },
                            { en: 'BB/9', zh: '每九局保送率', key: 'bb9' },
                            { en: 'K/BB', zh: '三振保送比', key: 'kbb' },
                            { en: 'BAA', zh: '被打擊率', key: 'baa' },
                            { en: 'SO', zh: '三振數', key: 'so' }
                        ];

                        if (params.name === '投手PR值') {
                            let result = '<strong>投手能力 PR值</strong><br/>';
                            statNames.forEach((stat) => {
                                const prValue = pitcherPR[stat.en] || 50;
                                const actualValue = pitcherRadarStats[stat.key] || 0;
                                const formatValue = stat.en === 'SO' 
                                    ? actualValue.toFixed(0) 
                                    : actualValue.toFixed(2);
                                result += `${stat.zh}(${stat.en}): ${formatValue} | PR${prValue.toFixed(1)}<br/>`;
                            });
                            return result;
                        } else if (params.name === '隊伍平均') {
                            let result = '<strong>隊伍平均</strong><br/>';
                            statNames.forEach((stat) => {
                                const avgValue = pitcherTeamAvg[stat.en] || 0;
                                const prValue = pitcherTeamPR?.[stat.en] || 50;
                                const formatValue = stat.en === 'SO' 
                                    ? avgValue.toFixed(0)
                                    : avgValue.toFixed(2);
                                result += `${stat.zh}(${stat.en}): ${formatValue} | PR${prValue.toFixed(1)}<br/>`;
                            });
                            return result;
                        } else {
                            let result = '<strong>聯盟中位數</strong><br/>';
                            statNames.forEach((stat) => {
                                const avgValue = pitcherMedianStats[stat.en] || 0;
                                const formatValue = stat.en === 'SO' 
                                    ? avgValue.toFixed(0)
                                    : avgValue.toFixed(2);
                                result += `${stat.zh}(${stat.en}): ${formatValue}<br/>`;
                            });
                            return result;
                        }
                    }
                },
                legend: { 
                    top: 25,
                    data: ['投手PR值', '隊伍平均', '聯盟中位數']
                },
                radar: {
                    indicator: [
                        { name: 'ERA(低為佳)', max: 100 },
                        { name: 'WHIP(低為佳)', max: 100 },
                        { name: 'K/9', max: 100 },
                        { name: 'BB/9(低為佳)', max: 100 },
                        { name: 'K/BB', max: 100 },
                        { name: 'BAA(低為佳)', max: 100 },
                        { name: 'SO', max: 100 }
                    ],
                    center: ['50%', '58%'],
                    radius: '60%'
                },
                series: [{
                    type: 'radar',
                    data: [
                        {
                            value: [
                                pitcherPR.ERA || 0,
                                pitcherPR.WHIP || 0,
                                pitcherPR.K9 || 0,
                                pitcherPR.BB9 || 0,
                                pitcherPR.KBBRatio || 0,
                                pitcherPR.BAA || 0,
                                pitcherPR.SO || 0
                            ],
                            name: '投手PR值',
                            areaStyle: { opacity: 0.3 },
                            lineStyle: { width: 2 },
                            itemStyle: { color: '#ee6666' }
                        },
                        {
                            value: [
                                pitcherTeamPR?.ERA || 50,
                                pitcherTeamPR?.WHIP || 50,
                                pitcherTeamPR?.K9 || 50,
                                pitcherTeamPR?.BB9 || 50,
                                pitcherTeamPR?.KBBRatio || 50,
                                pitcherTeamPR?.BAA || 50,
                                pitcherTeamPR?.SO || 50
                            ],
                            name: '隊伍平均',
                            lineStyle: { 
                                type: 'solid',
                                width: 1.5,
                                color: '#fac858'
                            },
                            itemStyle: { color: '#fac858' },
                            areaStyle: { opacity: 0 }
                        },
                        {
                            value: [50, 50, 50, 50, 50, 50, 50], // 聯盟中位數 (PR50)
                            name: '聯盟中位數',
                            lineStyle: { 
                                type: 'dashed',
                                width: 1.5,
                                color: '#91cc75'
                            },
                            itemStyle: { color: '#91cc75' },
                            areaStyle: { opacity: 0 }
                        }
                    ]
                }]
            });
        } else {
            // 沒有PR值資料時顯示錯誤提示
            pitcherRadarChart.setOption({
                title: { 
                    text: '無法顯示雷達圖', 
                    left: 'center', 
                    textStyle: { fontSize: 16, color: '#999' },
                    subtext: '缺少 PR 值數據',
                    subtextStyle: { fontSize: 12, color: '#999' }
                },
                graphic: {
                    type: 'text',
                    left: 'center',
                    top: 'middle',
                    style: {
                        text: '⚠ 數據不足\n無法計算百分位排名',
                        fontSize: 16,
                        fill: '#999',
                        textAlign: 'center'
                    }
                }
            });
        }
    }

    // 渲染圖表 (呼叫折線圖和雷達圖)

    // 初始化 DataTable
    function initDataTable() {
        $(document).ready(function () {
            if ($.fn.DataTable && !$.fn.DataTable.isDataTable('#battingTable')) {
                $('#battingTable').DataTable({
                    "language": {
                        "lengthMenu": "顯示 _MENU_ 筆資料",
                        "zeroRecords": "沒有找到資料",
                        "info": "顯示第 _START_ 至 _END_ 筆，共 _TOTAL_ 筆",
                        "infoEmpty": "沒有資料",
                        "infoFiltered": "(從 _MAX_ 筆資料過濾)",
                        "search": "搜尋:",
                        "paginate": {
                            "first": "第一頁",
                            "last": "最後一頁",
                            "next": "下一頁",
                            "previous": "上一頁"
                        }
                    },
                    "order": [[0, "asc"]],
                    "pageLength": 10,
                    "lengthChange": false,
                    "paging": true,
                    "searching": true,
                    "info": true
                });
            }
            
            if ($.fn.DataTable && !$.fn.DataTable.isDataTable('#pitchingTable')) {
                $('#pitchingTable').DataTable({
                    "language": {
                        "lengthMenu": "顯示 _MENU_ 筆資料",
                        "zeroRecords": "沒有找到資料",
                        "info": "顯示第 _START_ 至 _END_ 筆，共 _TOTAL_ 筆",
                        "infoEmpty": "沒有資料",
                        "infoFiltered": "(從 _MAX_ 筆資料過濾)",
                        "search": "搜尋:",
                        "paginate": {
                            "first": "第一頁",
                            "last": "最後一頁",
                            "next": "下一頁",
                            "previous": "上一頁"
                        }
                    },
                    "order": [[0, "asc"]],
                    "pageLength": 10,
                    "lengthChange": false,
                    "paging": true,
                    "searching": true,
                    "info": true
                });
            }
        });
    }

    // 設定賽季選擇器事件
    function setupSeasonSelector(playerId, actionUrl) {
        const selector = document.getElementById('seriesSelect');
        if (selector) {
            selector.addEventListener('change', function(e) {
                const seasonId = e.target.value;
                const url = actionUrl + '?playerId=' + encodeURIComponent(playerId) + '&seasonId=' + encodeURIComponent(seasonId);
                window.location.href = url;
            });
        }
    }

    // 渲染球種使用圓餅圖
    function renderPitchTypeChart(seasonId) {
        if (!pitchTypeChart) {
            return;
        }

        // 從表格中提取球種數據
        const pitchTypeRows = document.querySelectorAll('.table.table-hover tbody tr');
        const pitchTypeData = [];

        pitchTypeRows.forEach(row => {
            const cells = row.querySelectorAll('td');
            if (cells.length >= 4) {
                const pitchTypeName = cells[0].textContent?.split(' (')[0] || '';
                const usagePercentage = parseFloat(cells[2].textContent?.replace('%', '') || '0');
                
                if (usagePercentage > 0) {
                    pitchTypeData.push({
                        name: pitchTypeName,
                        value: usagePercentage
                    });
                }
            }
        });

        if (pitchTypeData.length === 0) {
            pitchTypeChart.setOption({
                title: { 
                    text: '球種使用統計', 
                    left: 'center', 
                    textStyle: { fontSize: 14 }
                },
                graphic: {
                    type: 'text',
                    left: 'center',
                    top: 'middle',
                    style: {
                        text: '⚠ 沒有球種數據',
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
                text: '球種使用比率', 
                left: 'center', 
                textStyle: { fontSize: 14 }
            },
            tooltip: {
                trigger: 'item',
                formatter: '{a} <br/>{b}: {c}% ({d}%)'
            },
            legend: {
                orient: 'vertical',
                left: 'left',
                top: 'middle',
                textStyle: {
                    fontSize: 12
                }
            },
            series: [{
                name: '球種使用',
                type: 'pie',
                radius: ['40%', '70%'],
                center: ['60%', '50%'],
                avoidLabelOverlap: false,
                emphasis: {
                    itemStyle: {
                        shadowBlur: 10,
                        shadowOffsetX: 0,
                        shadowColor: 'rgba(0, 0, 0, 0.5)'
                    }
                },
                label: {
                    show: false,
                    position: 'center'
                },
                labelLine: {
                    show: false
                },
                data: pitchTypeData
            }]
        };

        pitchTypeChart.setOption(option);
    }

    // 暴露公開方法
    window.PlayerDetailModule = {
        init: init,
        setupSeasonSelector: setupSeasonSelector,
        renderCharts: renderCharts,
        renderBatterLineChart: renderBatterLineChart,
        renderBatterRadarChart: renderBatterRadarChart,
        renderPitcherLineChart: renderPitcherLineChart,
        renderPitcherRadarChart: renderPitcherRadarChart,
        renderPitchTypeChart: renderPitchTypeChart
    };
})();
