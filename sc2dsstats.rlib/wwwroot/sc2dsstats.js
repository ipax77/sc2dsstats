
var replayModal = null;

Chart.plugins.register(ChartDataLabels);

function ReplayModalOpen(name) {

    const modalElement = document.getElementById(name);

    if (!modalElement) {
        return;
    }

    replayModal = new bootstrap.Modal(modalElement);
    replayModal.show();
}

function ReplayModalClose() {
    if (replayModal) {
        replayModal.hide();
    }
}

function HandleInputNavKeys(componentRef, inputRef) {
    console.log("subscribeToChange!");

    inputRef.onkeydown = function (event) {
        if (event.keyCode == "38") {
            event.preventDefault();
        }
        else if (event.keyCode == "40") {
            event.preventDefault();
        }
    };

    inputRef.onkeyup = function (event) {
        if (event.keyCode == "38") {
            event.preventDefault();
            componentRef.invokeMethodAsync('KeyPressed', event.keyCode);
        }
        else if (event.keyCode == "40") {
            event.preventDefault();
            componentRef.invokeMethodAsync('KeyPressed', event.keyCode);
        }
        else if (event.keyCode == "13") {
            componentRef.invokeMethodAsync('KeyPressed', event.keyCode);
        } else
        {
            componentRef.invokeMethodAsync('KeyPressed', 0);
        }
    };
};

function copyClipboard() {
    var copyText = document.querySelector("#input");
    copyText.select();
    document.execCommand("copy");
}


function ToggleClass(ele, name) {
    var element = document.getElementById(ele);
    element.classList.toggle(name);
}