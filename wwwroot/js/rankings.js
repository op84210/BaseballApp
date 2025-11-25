$(document).ready(function () {

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
        "order": [[9, "desc"]], // 預設按打擊率降序排列
        "pageLength": 10,
        "lengthChange": false,
        "columnDefs": [
            { "orderable": false, "targets": 0 } // 排名欄位不可排序
        ]
    });

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
        "order": [[9, "asc"]], // 預設按防禦率升序排列
        "pageLength": 10,
        "lengthChange": false,
        "columnDefs": [
            { "orderable": false, "targets": 0 } // 排名欄位不可排序
        ]
    });

});

// eCharts ranking chart
(function () {
    // 等待 DOMReady（也能與 jQuery ready 共存）
    function ready(fn) {
        if (document.readyState !== 'loading') fn();
        else document.addEventListener('DOMContentLoaded', fn);
    }

    ready(function () {
        if (typeof echarts === 'undefined') {
            // 如果 eCharts 尚未載入，什麼都不做
            return;
        }

        var container = document.getElementById('rankingChart');
        if (!container) return;

        var data = window.rankingData || { items: [] };
        var items = data.items || [];

        if (!items.length) {
            container.innerHTML = '<div class="text-center text-muted">目前沒有可顯示的排行榜資料</div>';
            return;
        }

        var isPitching = data.category === 'pitching';

        var combined = items.map(function (it) { return { name: it.name, value: parseFloat(it.value) }; });
        if (isPitching) combined.sort(function (a, b) { return a.value - b.value; });
        else combined.sort(function (a, b) { return b.value - a.value; });

        var maxDisplay = 10; // 只顯示前 10 筆
        if (combined.length > maxDisplay) combined = combined.slice(0, maxDisplay);

        var names = combined.map(function (c) { return c.name; });
        var values = combined.map(function (c) { return c.value; });

        var chart = echarts.init(container);

        var valueFormatter = function (v) {
            if (isPitching) return parseFloat(v).toFixed(2);
            return parseFloat(v).toFixed(3);
        };

        var option = {
            tooltip: {
                trigger: 'axis',
                axisPointer: { type: 'shadow' },
                formatter: function (params) {
                    if (!params || !params.length) return '';
                    var p = params[0];
                    return p.name + '<br/>' + p.seriesName + ': ' + valueFormatter(p.value);
                }
            },
            grid: { left: 40, right: 20, top: 60, bottom: 80 },
            xAxis: {
                type: 'category',
                data: names,
                axisLabel: {
                    interval: 0,
                    rotate: 30,
                    formatter: function (val) { return val; }
                }
            },
            yAxis: {
                type: 'value',
                name: isPitching ? 'ERA' : (data.category === 'batting' ? 'AVG' : '')
            },
            series: [
                {
                    name: isPitching ? 'ERA' : (data.category === 'batting' ? 'AVG' : 'value'),
                    type: 'bar',
                    data: values,
                    itemStyle: {
                        color: new echarts.graphic.LinearGradient(0, 0, 0, 1, [
                            { offset: 0, color: '#4facfe' },
                            { offset: 1, color: '#00f2fe' }
                        ])
                    },
                    label: {
                        show: true,
                        position: 'top',
                        formatter: function (p) { return valueFormatter(p.value); }
                    }
                }
            ]
        };

        chart.setOption(option);

        window.addEventListener('resize', function () { chart.resize(); });
    });
})();