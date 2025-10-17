document.addEventListener("DOMContentLoaded", async () => {
  const battingEl = document.getElementById("battingChart");
  const hrEl = document.getElementById("homerunChart");
  const radarEl = document.getElementById("radarChart");
  const battingChart = echarts.init(battingEl);
  const hrChart = echarts.init(hrEl);
  const radarChart = echarts.init(radarEl);

  // 打擊率趨勢
  const bt = await fetch("/api/chartsapi/batting-trend?playerId=demo").then(r=>r.json());
  battingChart.setOption({
    tooltip:{ trigger:"axis" },
    xAxis:{ type:"category", data: bt.labels },
    yAxis:{ type:"value" },
    series:[{ type:"line", data: bt.series, smooth:true, name:"AVG" }]
  });

  // 全壘打統計
  const hrs = await fetch("/api/chartsapi/homeruns?season=2024&top=10").then(r=>r.json());
  hrChart.setOption({
    tooltip:{ trigger:"axis" },
    xAxis:{ type:"category", data: hrs.map(x=>x.player) },
    yAxis:{ type:"value" },
    series:[{ type:"bar", data: hrs.map(x=>x.hr), name:"HR" }]
  });

  // 雷達圖
  const rd = await fetch("/api/chartsapi/radar?playerId=demo&season=2024").then(r=>r.json());
  radarChart.setOption({
    tooltip:{},
    radar:{ indicator: rd.indicators.map(n=>({ name:n, max:100 })) },
    series:[{ type:"radar", data:[{ value: rd.values, name:"Player" }] }]
  });

  window.addEventListener("resize", () => {
    battingChart.resize(); hrChart.resize(); radarChart.resize();
  });
});