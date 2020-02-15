Chart.plugins.unregister(ChartDataLabels);

window.DynChart = (chartdata) => {
    var mychart = JSON.parse(chartdata);
    if (mychart != null) {
        var can = document.getElementById('canvas');
        if (can != null) {
            var ctx = document.getElementById('canvas').getContext('2d');
            if (window.myChart != null) {
                window.myChart.destroy();
            }
            if (mychart && mychart["options"] && mychart["options"]["plugins"]) {
                if (mychart.type == "bar") {
                    mychart["options"]["plugins"]["datalabels"].display = window[mychart.myChartDataLabels];
                    mychart["plugins"] = [ChartDataLabels];
                }
            }
            //mychart.type = "horizontalBar";
            window.myChart = new Chart(ctx, mychart);
        }
        
    }
}

window.AddDynChart = (chartdataset) => {
    var mychartdataset = JSON.parse(chartdataset);
    if (window.myChart != null) {
        window.myChart.data.datasets.push(mychartdataset);
        window.myChart.update();
    }
}

window.RemoveDynChart = (chartdatasetpos) => {
    if (window.myChart != null) {
        window.myChart.data.datasets.splice(chartdatasetpos, 1);
        window.myChart.update();
    }
}

window.AddData = (label, winrate, dcolor, dimage) => {
    if (window.myChart != null) {
        if (window.myChart.data.datasets.length == 1) {
            window.myChart.data.labels.push(label);
            window.myChart.data.datasets[window.myChart.data.datasets.length - 1].data.push(winrate);
            window.myChart.data.datasets[window.myChart.data.datasets.length - 1].backgroundColor.push(dcolor);
            if (dimage > '') {
                window.myChart.options.plugins.labels.images.push(JSON.parse(dimage));
            }
        } else {
            window.myChart.data.datasets[window.myChart.data.datasets.length - 1].data.push(winrate);
            window.myChart.data.datasets[window.myChart.data.datasets.length - 1].backgroundColor.push(dcolor);
        }
        window.myChart.update();
    }
}

window.ChangeOptionsDynChart = (chchartoptions) => {
    if (window.myChart != null) {
        window.myChart.options = JSON.parse(chchartoptions);
        window.myChart.update();
    }
}

window.SortChart = (mlabels, mdatasetdata, mimages, mcolors) => {

    if (window.myChart != null && window.myChart.data.datasets.length > 0) {
        window.myChart.data.labels = JSON.parse(mlabels);
        window.myChart.data.datasets[0].data = JSON.parse(mdatasetdata);
        window.myChart.data.datasets[0].backgroundColor = JSON.parse(mcolors);
        if (mimages > '') {
            window.myChart.options.plugins.labels.images = JSON.parse(mimages);
        }
        window.myChart.update();
    }
}

window.CopyToClipboard = (element) => {
    var can = document.getElementById(element);
    if (can != null) {
        if (element == "canvas") {
            return can.toDataURL()
        } else {
            html2canvas(can, { backgroundColor: "#272b30" }).then(function (canvas) {
                canvas.toBlob(function (blob) {
                    const item = new ClipboardItem({ "image/png": blob });
                    navigator.clipboard.write([item]);
                });
            });
        }

    }
}


window.CopyToClipboard_chrome = (element) => {
    var can = document.getElementById(element);
    if (can != null) {
        if (element == "canvas") {
            var ctx = can.getContext("2d");
            ctx.globalCompositeOperation = 'destination-over'
            ctx.fillStyle = "#272b30";
            ctx.fillRect(0, 0, canvas.width, canvas.height);
            can.toBlob(function (blob) {
                const item = new ClipboardItem({ "image/png": blob });
                navigator.clipboard.write([item]);
            });
        } else {
            html2canvas(can, {backgroundColor: "#272b30"}).then(function (canvas) {
                canvas.toBlob(function (blob) {
                    const item = new ClipboardItem({ "image/png": blob });
                    navigator.clipboard.write([item]);
                });
            });
        }

    }
}

window.PieChart = (piechart) => {
    var piecan = document.getElementById('outlabeledChart');
    if (piecan != null) {
        var ctx = document.getElementById('outlabeledChart').getContext('2d');
        var config = {
            plugins: [ChartDataLabels],
            type: 'pie',
			    data: {
				    datasets: [{
					    data: [
					    ],
					    backgroundColor: [
					    ],
					    label: 'Commanders'
				    }],
				    labels: [
				    ]
			    },
            options: {
                responsive: true,
                legend: {
                    position: 'right',
                    labels: {
                        fontSize: 14,
                        fontColor: "#eaffff"
                    }
                },
                plugins: {
                    // Change options for ALL labels of THIS CHART
                    datalabels: {
                        color: '#eafff',
                        display: 'auto'
                    },
                    labels: {
                        position: 'outside',
                        showActualPercentages: true,
                        fontColor: '#eafff',
                        outsidePadding: 4,
                        textMargin: 4
                    }
                }
            }

		    };
        config.data.datasets[0].data = piechart.piedata;
        config.data.datasets[0].backgroundColor = piechart.piecolors;
        config.data.labels = piechart.pielabels;
        config.options.plugins.datalabels.formatter = function (value, context) {
            var index = context.dataIndex;
            var myvalue = piechart.pielabels[index];
            return myvalue + '\n' + value;
        };
        if (window.myPieChart != null) {
            window.myPieChart.destroy();
        }
    
        window.myPieChart = new Chart(ctx, config);
    }
}

window.DummyChart = (dummychart) => {
    var dummycan = document.getElementById('dummychart');
    if (dummycan != null) {
        var ctx = document.getElementById('dummychart').getContext('2d');
        var ctx2 = document.getElementById('dummy').getContext('2d');
var bar_config = {
    type: 'bar',
    data: {
        labels: ["Abathur","Alarak","Artanis","Dehaka","Fenix","Horner","Karax","Kerrigan","Nova","Raynor","Stukov","Swann","Tychus","Vorazun","Zagara"],
        datasets: []
    },
options: {
responsive: true,
tooltips: false,
title: {
display: true,
fontSize: 22,
fontColor: "#eaffff",
text: 'Winrate',
position: 'top'
},
layout: {
padding: 24
},
elements: {
rectangle: {
backgroundColor: "cc55aa"
}
},
legend: {
position: 'top',
labels: {
fontSize: 14,
fontColor: "#eaffff"
}
},
plugins: {
datalabels: {
align: 'end',
anchor: 'end',
formatter: function(value, context) {
return context.dataset.icons[context.dataIndex];
}
}
}
}
};
        var gradient = ctx2.createLinearGradient(0, 0, 0, 500);
        gradient.addColorStop(0, '#070084');
        gradient.addColorStop(1, '#750016');        
        var mydataset = [{
            label: 'Dataset 1',
            backgroundColor: [
                '#0000ff'
            ],
				borderColor: '#eaffff',
				borderWidth: 1,
				data: ["42.86","48.84","42.86","47.69","51.92","52.78","70.18","50.00","47.83","44.26","25.81","59.09","54.93","56.10","43.48"]
	}];
    bar_config.data.datasets = mydataset;

    var mylabels = {
    render: 'image',
    images: [
      { src: 'images/btn-unit-hero-nova.png', width: 30, height: 30 },
      { src: 'images/btn-unit-hero-nova.png', width: 30, height: 30 },
      { src: 'images/btn-unit-hero-nova.png', width: 30, height: 30 },
      { src: 'images/btn-unit-hero-nova.png', width: 30, height: 30 },
      { src: 'images/btn-unit-hero-nova.png', width: 30, height: 30 },
      { src: 'images/btn-unit-hero-nova.png', width: 30, height: 30 },
      { src: 'images/btn-unit-hero-nova.png', width: 30, height: 30 },
      { src: 'images/btn-unit-hero-nova.png', width: 30, height: 30 },
      { src: 'images/btn-unit-hero-nova.png', width: 30, height: 30 },
      { src: 'images/btn-unit-hero-nova.png', width: 30, height: 30 },
      { src: 'images/btn-unit-hero-nova.png', width: 30, height: 30 },
      { src: 'images/btn-unit-hero-nova.png', width: 30, height: 30 },
      { src: 'images/btn-unit-hero-nova.png', width: 30, height: 30 },
      { src: 'images/btn-unit-hero-nova.png', width: 30, height: 30 },
      { src: 'images/btn-unit-hero-nova.png', width: 30, height: 30 }
    ]
    };    
    bar_config.options.plugins.labels = mylabels;

    var mydatalabel = {
                color: '#36A2EB',
                align: 'bottom',
                anchor: 'end'
    };
    bar_config.plugins = [ChartDataLabels];
    bar_config.options.plugins.datalabels = mydatalabel;
        
    window.myDummyChart = new Chart(ctx, bar_config);
    }
}

window.myChartDataLabels = function (context) {
    return context.dataset.data[context.dataIndex] > 15;
}

function SelectText(element) {
    var doc = document;
    if (doc.body.createTextRange) {
        var range = document.body.createTextRange();
        range.moveToElementText(element);
        range.select();
    } else if (window.getSelection) {
        var selection = window.getSelection();
        var range = document.createRange();
        range.selectNodeContents(element);
        selection.removeAllRanges();
        selection.addRange(range);
    }
}