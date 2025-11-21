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