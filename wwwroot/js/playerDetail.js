// PlayerDetail 頁面的圖表與表格管理
(function () {
    'use strict';

    let lineChart = null;
    let radarChart = null;
    let gameStatsData = [];
    let percentileRanksData = {};
    let seasonAveragesData = {};

    // 初始化函數
    function init(gameStats, seasonId, percentileRanks, seasonAverages) {
        gameStatsData = gameStats || [];
        percentileRanksData = percentileRanks || {};
        seasonAveragesData = seasonAverages || {};
        
        // 初始化圖表
        lineChart = echarts.init(document.getElementById('lineChart'));
        radarChart = echarts.init(document.getElementById('radarChart'));

        // 渲染圖表
        if (seasonId) {
            renderCharts(seasonId);
        }

        // 設定視窗大小調整事件
        window.addEventListener('resize', function() {
            if (lineChart) lineChart.resize();
            if (radarChart) radarChart.resize();
        });

        // 初始化 DataTable
        initDataTable();
    }

    // 計算統計數據
    function calculateStats(games) {
        if (!games || games.length === 0) return null;

        const totalAB = games.reduce((sum, g) => sum + g.AB, 0);
        const totalH = games.reduce((sum, g) => sum + g.H, 0);
        const total1B = games.reduce((sum, g) => sum + g._1B, 0);
        const total2B = games.reduce((sum, g) => sum + g._2B, 0);
        const total3B = games.reduce((sum, g) => sum + g._3B, 0);
        const totalHR = games.reduce((sum, g) => sum + g.HR, 0);
        const totalBB = games.reduce((sum, g) => sum + g.BB, 0);
        const totalHBP = games.reduce((sum, g) => sum + g.HBP, 0);
        const totalSF = games.reduce((sum, g) => sum + g.SF, 0);
        const totalSO = games.reduce((sum, g) => sum + g.SO, 0);
        const totalRBI = games.reduce((sum, g) => sum + g.RBI, 0);

        const AVG = totalAB > 0 ? totalH / totalAB : 0;
        const OBP = (totalAB + totalBB + totalHBP + totalSF) > 0
            ? (totalH + totalBB + totalHBP) / (totalAB + totalBB + totalHBP + totalSF)
            : 0;
        const totalBases = total1B + (total2B * 2) + (total3B * 3) + (totalHR * 4);
        const SLG = totalAB > 0 ? totalBases / totalAB : 0;
        const OPS = OBP + SLG;

        return { AVG, OBP, SLG, OPS, HR: totalHR, RBI: totalRBI, SO: totalSO, BB: totalBB };
    }

    // 渲染圖表
    function renderCharts(seasonId) {
        if (!seasonId || !lineChart || !radarChart) {
            return;
        }

        const filteredGames = gameStatsData;

        // 累積統計
        let cumulative = { AB: 0, H: 0, BB: 0, HBP: 0, SF: 0, _1B: 0, _2B: 0, _3B: 0, HR: 0, IHR: 0 };
        const avgData = [];
        const opsData = [];

        filteredGames.forEach(g => {
            cumulative.AB += g.AB;
            cumulative.H += g.H;
            cumulative.BB += g.BB;
            cumulative.HBP += g.HBP;
            cumulative.SF += g.SF;
            cumulative._1B += g._1B;
            cumulative._2B += g._2B;
            cumulative._3B += g._3B;
            cumulative.HR += g.HR;
            cumulative.IHR += g.IHR;

            const avg = cumulative.AB > 0 ? cumulative.H / cumulative.AB : 0;
            const obpDen = cumulative.AB + cumulative.BB + cumulative.HBP + cumulative.SF;
            const obp = obpDen > 0 ? (cumulative.H + cumulative.BB + cumulative.HBP) / obpDen : 0;
            const totalBases = cumulative._1B + cumulative._2B * 2 + cumulative._3B * 3 + (cumulative.HR + cumulative.IHR) * 4;
            const slg = cumulative.AB > 0 ? totalBases / cumulative.AB : 0;
            const ops = obp + slg;
            avgData.push(+avg.toFixed(3));
            opsData.push(+ops.toFixed(3));
        });

        // 折線圖配置
        lineChart.setOption({
            title: { text: '累積 OPS / AVG 趨勢', left: 'center', textStyle: { fontSize: 14 } },
            tooltip: { trigger: 'axis' },
            legend: { top: 25 },
            grid: { left: 55, right: 25, top: 70, bottom: 40 },
            xAxis: {
                type: 'category',
                data: filteredGames.map(g => new Date(g.Date).toLocaleDateString('zh-TW', { month: 'numeric', day: 'numeric' }))
            },
            yAxis: { type: 'value', name: '值' },
            series: [
                {
                    name: '累積OPS',
                    type: 'line',
                    smooth: true,
                    data: opsData,
                    areaStyle: { opacity: 0.1 }
                },
                {
                    name: '累積AVG',
                    type: 'line',
                    smooth: true,
                    data: avgData,
                    areaStyle: { opacity: 0.1 }
                }
            ]
        });

        // 雷達圖配置 - 顯示PR值
        if (percentileRanksData && Object.keys(percentileRanksData).length > 0) {
            const prIndicators = [
                { name: 'AVG', max: 100 },
                { name: 'OBP', max: 100 },
                { name: 'SLG', max: 100 },
                { name: 'OPS', max: 100 },
                { name: 'HR', max: 100 },
                { name: 'RBI', max: 100 },
                { name: 'SO(少為佳)', max: 100 },
                { name: 'BB', max: 100 }
            ];

            // 球員PR值資料
            const playerPRValues = [
                percentileRanksData.AVG || 0,
                percentileRanksData.OBP || 0,
                percentileRanksData.SLG || 0,
                percentileRanksData.OPS || 0,
                percentileRanksData.HR || 0,
                percentileRanksData.RBI || 0,
                percentileRanksData.SO || 0,
                percentileRanksData.BB || 0
            ];

            // 賽季平均PR值 (50%)
            const averagePRValues = [50, 50, 50, 50, 50, 50, 50, 50];

            radarChart.setOption({
                title: { 
                    text: '能力PR值雷達圖', 
                    left: 'center', 
                    textStyle: { fontSize: 14 },
                    subtext: 'PR值: 百分位排名 (0-100), 虛線為賽季平均 (PR50)',
                    subtextStyle: { fontSize: 11, color: '#999' }
                },
                tooltip: {
                    trigger: 'item',
                    formatter: function(params) {
                        if (params.seriesName === '球員PR值') {
                            const prValue = params.value[params.dataIndex];
                            return `${prIndicators[params.dataIndex].name}: PR${prValue.toFixed(1)} (勝過${prValue.toFixed(1)}%的球員)`;
                        } else {
                            return `賽季平均: PR50`;
                        }
                    }
                },
                legend: { 
                    top: 50,
                    data: ['球員PR值', '賽季平均']
                },
                radar: { 
                    indicator: prIndicators,
                    center: ['50%', '58%'],
                    radius: '60%'
                },
                series: [{
                    type: 'radar',
                    data: [
                        {
                            value: playerPRValues,
                            name: '球員PR值',
                            areaStyle: { opacity: 0.3 },
                            lineStyle: { width: 2 },
                            itemStyle: { color: '#5470c6' }
                        },
                        {
                            value: averagePRValues,
                            name: '賽季平均',
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
            // 如果沒有PR值資料,顯示原始統計值雷達圖
            const stats = calculateStats(filteredGames);
            if (stats) {
                const indicators = [
                    { name: 'AVG', max: 1 },
                    { name: 'OBP', max: 1 },
                    { name: 'SLG', max: 2 },
                    { name: 'HR', max: Math.max((stats.HR || 0) * 1.3, 1) },
                    { name: 'RBI', max: Math.max((stats.RBI || 0) * 1.3, 1) },
                    { name: 'SO(少為佳)', max: Math.max((stats.SO || 0) * 1.3, 5) }
                ];

                radarChart.setOption({
                    title: { text: '能力雷達', left: 'center', textStyle: { fontSize: 14 } },
                    tooltip: {},
                    radar: { indicator: indicators },
                    series: [{
                        type: 'radar',
                        data: [{
                            value: [
                                stats.AVG || 0,
                                stats.OBP || 0,
                                stats.SLG || 0,
                                stats.HR || 0,
                                stats.RBI || 0,
                                (stats.SO ? (indicators[5].max - stats.SO) : 0)
                            ],
                            name: '球員數據'
                        }],
                        areaStyle: { opacity: 0.3 }
                    }]
                });
            }
        }
    }

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

    // 暴露公開方法
    window.PlayerDetailModule = {
        init: init,
        setupSeasonSelector: setupSeasonSelector,
        renderCharts: renderCharts
    };
})();
