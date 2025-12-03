// PlayerDetail 頁面的圖表與表格管理
(function () {
    'use strict';
    
    // 全域變數
    let batterLineChart = null;
    let batterRadarChart = null;
    let pitcherLineChart = null;
    let pitcherRadarChart = null;

    // 打者數據變數
    let batterGames = [];
    let batterPR = {};
    let batterSeasonAvg = {};
    let batterTeamAvg = {};
    let batterTeamPR = {};

    // 投手數據變數
    let pitcherGames = [];
    let pitcherPR = {};
    let pitcherSeasonAvg = {};
    let pitcherTeamAvg = {};
    let pitcherTeamPR = {};

    // 初始化函數（新版：支援 API 載入）
    function init(playerId, seasonId) {
        // 初始化圖表容器
        const batterLineChartElement = document.getElementById('batterLineChart');
        const batterRadarChartElement = document.getElementById('batterRadarChart');
        const pitcherLineChartElement = document.getElementById('pitcherLineChart');
        const pitcherRadarChartElement = document.getElementById('pitcherRadarChart');

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

            if (batterResponse.ok) {
                const batterData = await batterResponse.json();
                batterGames = batterData.chartData || [];
                batterPR = batterData.percentileRanks || {};
                batterSeasonAvg = batterData.seasonAverages || {};
                batterTeamAvg = batterData.teamAverages || {};
                batterTeamPR = batterData.teamPercentileRanks || {};
            }

            if (pitcherResponse.ok) {
                const pitcherData = await pitcherResponse.json();
                pitcherGames = pitcherData.chartData || [];
                pitcherPR = pitcherData.percentileRanks || {};
                pitcherSeasonAvg = pitcherData.seasonAverages || {};
                pitcherTeamAvg = pitcherData.teamAverages || {};
                pitcherTeamPR = pitcherData.teamPercentileRanks || {};
            }

            // 渲染所有圖表
            renderCharts(seasonId);
        
        } catch (error) {
            console.error('載入圖表數據失敗:', error);
        }
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

    // 渲染折線圖
    function renderBatterLineChart(seasonId) {
        if (!seasonId || !batterLineChart) {
            return;
        }

        // 累積統計
        let cumulative = { AB: 0, H: 0, BB: 0, HBP: 0, SF: 0, _1B: 0, _2B: 0, _3B: 0, HR: 0, IHR: 0 };
        const avgData = [];
        const opsData = [];

        batterGames.forEach(g => {
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
        batterLineChart.setOption({
            title: { text: '累積 OPS / AVG 趨勢', left: 'center', textStyle: { fontSize: 14 } },
            tooltip: { trigger: 'axis' },
            legend: { top: 25 },
            grid: { left: 55, right: 25, top: 70, bottom: 40 },
            xAxis: {
                type: 'category',
                data: batterGames.map(g => new Date(g.Date).toLocaleDateString('zh-TW', { month: 'numeric', day: 'numeric' }))
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
    }

    // 渲染雷達圖
    function renderBatterRadarChart(seasonId) {
        if (!seasonId || !batterRadarChart) {
            return;
        }

        // 雷達圖配置 - 顯示PR值
        if (batterPR && Object.keys(batterPR).length > 0) {

            // 計算球員實際數值
            const stats = calculateStats(batterGames);

            batterRadarChart.setOption({
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
                        const statNames = [
                            { en: 'AVG', zh: '打擊率', key: 'AVG' },
                            { en: 'OBP', zh: '上壘率', key: 'OBP' },
                            { en: 'SLG', zh: '長打率', key: 'SLG' },
                            { en: 'OPS', zh: 'OPS', key: 'OPS' },
                            { en: 'HR', zh: '全壘打', key: 'HR' },
                            { en: 'RBI', zh: '打點', key: 'RBI' },
                            { en: 'SO', zh: '三振', key: 'SO' },
                            { en: 'BB', zh: '保送', key: 'BB' }
                        ];

                        if (params.name === '球員PR值') {
                            let result = '<strong>球員能力 PR值</strong><br/>';
                            statNames.forEach((stat) => {
                                const prValue = batterPR[stat.key] || 0;
                                const actualValue = stats?.[stat.key] || 0;
                                const formatValue = (stat.en === 'HR' || stat.en === 'RBI' || stat.en === 'SO' || stat.en === 'BB') 
                                    ? actualValue.toFixed(0) 
                                    : actualValue.toFixed(3);
                                result += `${stat.zh}(${stat.en}): ${formatValue} | PR${prValue.toFixed(1)}<br/>`;
                            });
                            return result;
                        } else if (params.name === '隊伍平均') {
                            let result = '<strong>隊伍平均</strong><br/>';
                            statNames.forEach((stat) => {
                                const avgValue = batterTeamAvg[stat.key] || 0;
                                const prValue = batterTeamPR?.[stat.key] || 50;
                                const formatValue = (stat.en === 'HR' || stat.en === 'RBI' || stat.en === 'SO' || stat.en === 'BB') 
                                    ? avgValue.toFixed(0)
                                    : avgValue.toFixed(3);
                                result += `${stat.zh}(${stat.en}): ${formatValue} | PR${prValue.toFixed(1)}<br/>`;
                            });
                            return result;
                        } else {
                            let result = '<strong>賽季平均</strong><br/>';
                            statNames.forEach((stat) => {
                                const avgValue = batterSeasonAvg[stat.key] || 0;
                                const formatValue = (stat.en === 'HR' || stat.en === 'RBI' || stat.en === 'SO' || stat.en === 'BB') 
                                    ? avgValue.toFixed(0)
                                    : avgValue.toFixed(3);
                                result += `${stat.zh}(${stat.en}): ${formatValue}<br/>`;
                            });
                            return result;
                        }
                    }
                },
                legend: { 
                    top: 50,
                    data: ['球員PR值', '隊伍平均', '賽季平均']
                },
                radar: {
                    indicator: [
                        { name: 'AVG', max: 100 },
                        { name: 'OBP', max: 100 },
                        { name: 'SLG', max: 100 },
                        { name: 'OPS', max: 100 },
                        { name: 'HR', max: 100 },
                        { name: 'RBI', max: 100 },
                        { name: 'SO(少為佳)', max: 100 },
                        { name: 'BB', max: 100 }
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
                                batterPR.HR || 0,
                                batterPR.RBI || 0,
                                batterPR.SO || 0,
                                batterPR.BB || 0
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
                                batterTeamPR?.HR || 50,
                                batterTeamPR?.RBI || 50,
                                batterTeamPR?.SO || 50,
                                batterTeamPR?.BB || 50
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
                            value:  [50, 50, 50, 50, 50, 50, 50, 50],  // 賽季平均PR值 (50%)
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
    }

    // 渲染投手折線圖
    function renderPitcherLineChart(seasonId) {
        if (!seasonId || !pitcherLineChart) {
            return;
        }

        // 累積統計
        let cumulative = { IPOuts: 0, ER: 0, H: 0, BB: 0 };
        const eraData = [];
        const whipData = [];

        pitcherGames.forEach(g => {
            cumulative.IPOuts += g.IPOuts || 0;
            cumulative.ER += g.ER || 0;
            cumulative.H += g.H || 0;
            cumulative.BB += g.BB || 0;

            const era = cumulative.IPOuts > 0 ? (cumulative.ER * 27 / cumulative.IPOuts) : 0;
            const whip = cumulative.IPOuts > 0 ? ((cumulative.H + cumulative.BB) * 3 / cumulative.IPOuts) : 0;
            
            eraData.push(+era.toFixed(2));
            whipData.push(+whip.toFixed(2));
        });

        // 折線圖配置
        pitcherLineChart.setOption({
            title: { text: '累積 ERA / WHIP 趨勢', left: 'center', textStyle: { fontSize: 14 } },
            tooltip: { trigger: 'axis' },
            legend: { top: 25 },
            grid: { left: 55, right: 25, top: 70, bottom: 40 },
            xAxis: {
                type: 'category',
                data: pitcherGames.map(g => new Date(g.Date).toLocaleDateString('zh-TW', { month: 'numeric', day: 'numeric' }))
            },
            yAxis: { type: 'value', name: '值' },
            series: [
                {
                    name: '累積ERA',
                    type: 'line',
                    smooth: true,
                    data: eraData,
                    areaStyle: { opacity: 0.1 }
                },
                {
                    name: '累積WHIP',
                    type: 'line',
                    smooth: true,
                    data: whipData,
                    areaStyle: { opacity: 0.1 }
                }
            ]
        });
    }

    // 渲染投手雷達圖
    function renderPitcherRadarChart(seasonId) {
        if (!seasonId || !pitcherRadarChart) {
            return;
        }

        // 計算投手統計數據
        function calculatePitcherStats(games) {
            if (!games || games.length === 0) return null;

            const totalIPOuts = games.reduce((sum, g) => sum + (g.IPOuts || 0), 0);
            const totalER = games.reduce((sum, g) => sum + (g.ER || 0), 0);
            const totalH = games.reduce((sum, g) => sum + (g.H || 0), 0);
            const totalBB = games.reduce((sum, g) => sum + (g.BB || 0), 0);
            const totalSO = games.reduce((sum, g) => sum + (g.SO || 0), 0);
            const totalHR = games.reduce((sum, g) => sum + (g.HR || 0), 0);
            const totalBF = games.reduce((sum, g) => sum + (g.BF || 0), 0);

            const ERA = totalIPOuts > 0 ? (totalER * 27 / totalIPOuts) : 0;
            const WHIP = totalIPOuts > 0 ? ((totalH + totalBB) * 3 / totalIPOuts) : 0;
            const K9 = totalIPOuts > 0 ? (totalSO * 27 / totalIPOuts) : 0;
            const BB9 = totalIPOuts > 0 ? (totalBB * 27 / totalIPOuts) : 0;
            const KBB = totalBB > 0 ? (totalSO / totalBB) : totalSO;
            const opponentAB = totalBF - totalBB - (games.reduce((sum, g) => sum + (g.HBP || 0), 0));
            const BAA = opponentAB > 0 ? (totalH / opponentAB) : 0;

            return { ERA, WHIP, K9, BB9, KBB, BAA, SO: totalSO, BB: totalBB };
        }

        // 雷達圖配置 - 顯示PR值
        if (pitcherPR && Object.keys(pitcherPR).length > 0) {

            // 計算投手實際數值
            const stats = calculatePitcherStats(pitcherGames);
            
            pitcherRadarChart.setOption({
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
                        const statNames = [
                            { en: 'ERA', zh: '防禦率', key: 'ERA' },
                            { en: 'WHIP', zh: 'WHIP', key: 'WHIP' },
                            { en: 'K/9', zh: '每九局三振率', key: 'K9' },
                            { en: 'BB/9', zh: '每九局保送率', key: 'BB9' },
                            { en: 'K/BB', zh: '三振保送比', key: 'KBBRatio' },
                            { en: 'BAA', zh: '被打擊率', key: 'BAA' },
                            { en: 'SO', zh: '三振數', key: 'SO' }
                        ];

                        if (params.name === '投手PR值') {
                            let result = '<strong>投手能力 PR值</strong><br/>';
                            statNames.forEach((stat) => {
                                const prValue = pitcherPR[stat.key] || 50;
                                const actualValue = stats?.[stat.key] || 0;
                                const formatValue = stat.en === 'SO' 
                                    ? actualValue.toFixed(0) 
                                    : actualValue.toFixed(2);
                                result += `${stat.zh}(${stat.en}): ${formatValue} | PR${prValue.toFixed(1)}<br/>`;
                            });
                            return result;
                        } else if (params.name === '隊伍平均') {
                            let result = '<strong>隊伍平均</strong><br/>';
                            statNames.forEach((stat) => {
                                const avgValue = pitcherTeamAvg[stat.key] || 0;
                                const prValue = pitcherTeamPR?.[stat.key] || 50;
                                const formatValue = stat.en === 'SO' 
                                    ? avgValue.toFixed(0)
                                    : avgValue.toFixed(2);
                                result += `${stat.zh}(${stat.en}): ${formatValue} | PR${prValue.toFixed(1)}<br/>`;
                            });
                            return result;
                        } else {
                            let result = '<strong>賽季平均</strong><br/>';
                            statNames.forEach((stat) => {
                                const avgValue = pitcherSeasonAvg[stat.key] || 0;
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
                    top: 50,
                    data: ['投手PR值', '隊伍平均', '賽季平均']
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
                            value: [50, 50, 50, 50, 50, 50, 50], // 賽季平均PR值 (50%)
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

    // 暴露公開方法
    window.PlayerDetailModule = {
        init: init,
        setupSeasonSelector: setupSeasonSelector,
        renderCharts: renderCharts,
        renderBatterLineChart: renderBatterLineChart,
        renderBatterRadarChart: renderBatterRadarChart,
        renderPitcherLineChart: renderPitcherLineChart,
        renderPitcherRadarChart: renderPitcherRadarChart
    };
})();
