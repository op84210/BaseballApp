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

        // 使用後端計算的數據
        const avgData = batterGames.map(g => +g.avgData.toFixed(3));
        const opsData = batterGames.map(g => +g.opsData.toFixed(3));

        // 折線圖配置
        batterLineChart.setOption({
            title: { text: '累積 OPS / AVG 趨勢', left: 'center', textStyle: { fontSize: 14 } },
            tooltip: { trigger: 'axis' },
            legend: { top: 25 },
            grid: { left: 55, right: 25, top: 70, bottom: 40 },
            xAxis: {
                type: 'category',
                data: batterGames.map(g => new Date(g.date).toLocaleDateString('zh-TW', { month: 'numeric', day: 'numeric' }))
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
                    subtext: 'PR值: 百分位排名 (0-100), 虛線為聯盟中位數 (PR50)',
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
                    top: 50,
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

        // 折線圖配置
        pitcherLineChart.setOption({
            title: { text: '累積 ERA / WHIP 趨勢', left: 'center', textStyle: { fontSize: 14 } },
            tooltip: { trigger: 'axis' },
            legend: { top: 25 },
            grid: { left: 55, right: 25, top: 70, bottom: 40 },
            xAxis: {
                type: 'category',
                data: pitcherGames.map(g => new Date(g.date).toLocaleDateString('zh-TW', { month: 'numeric', day: 'numeric' }))
            },
            yAxis: { type: 'value', name: '值' },
            series: [
                {
                    name: '累積ERA',
                    type: 'line',
                    smooth: true,
                    data: pitcherGames.map(g => +g.era.toFixed(2)),
                    areaStyle: { opacity: 0.1 }
                },
                {
                    name: '累積WHIP',
                    type: 'line',
                    smooth: true,
                    data: pitcherGames.map(g => +g.whip.toFixed(2)),
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
                    subtext: 'PR值: 百分位排名 (0-100), 虛線為聯盟中位數 (PR50)',
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
                    top: 50,
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
