function testBetfair()
{
    var debugMode = selectElement = document.querySelector('#selectDebug').value;

    var url = "/Home/getBfData?debugMode=" + debugMode;

    showProgress_map_upload();
    $.ajax({
        url: url,
        type: 'GET',
        dataType: "json",
        success: function (e) {

            const myJSON = JSON.stringify(e);

            document.getElementById("formmessage").innerText = JSON.stringify(e);
            hideProgress_map_upload();
        },
        error: function (e) {

            console.log('ERROR Ajax' + JSON.stringify(e));
            document.getElementById("formmessage").innerText = JSON.stringify(e);
            hideProgress_map_upload();
        }
    });
}

function showProgress_map_upload() {

    document.getElementById("loading").style.display = "block";
}

function hideProgress_map_upload() {
    document.getElementById("loading").style.display = "none";
}
