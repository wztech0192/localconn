var port = ":1998/";

var ws = null;
var lblConn = document.getElementById('conn');
var lblTarget = document.getElementById('target');
var cmd = document.getElementById('cmd');
var cmdTitle = document.getElementById('cmd-title');
var output_field = document.getElementById('output');
var leftjoy = document.getElementById('leftjoy');
var centerP = document.getElementById('centerP');
var ipInput = document.getElementById('ip');

ipInput.value =  getCookie('ip');



ipInput.onchange = function(){
    document.cookie = "ip=" + (this.value)  + "; path=/";
}

function getCookie(name) {
    var nameEQ = name + "=";
    var ca = document.cookie.split(';');
    for(var i=0;i < ca.length;i++) {
        var c = ca[i];
        while (c.charAt(0)==' ') c = c.substring(1,c.length);
        if (c.indexOf(nameEQ) == 0) return c.substring(nameEQ.length,c.length);
    }
    return null;
}


document.getElementById('toggle').onclick = function () {
    if (ws === null) {
        var ip = ipInput.value;
        if( ip==='') return alert("IP cannot be empty");
        ws = new WebSocket('ws://'+ip+port);
        output("Connecting...............");
        startConn();
    } else {
        ws.close();
        ws = null;
        toggleConn(lblConn, "OFF");
    }
};

document.getElementById('send_cmd').addEventListener('touchstart', function (e) {
    e.preventDefault();
    var val = (cmdTitle.value === "" ? "" : (cmdTitle.value + "\n")) + cmd.value;
    cmd.value = "";
    send('cmd&' + val); //send method
    output(val);
},false);

document.getElementById('tcw').addEventListener('touchstart', function (e) {
    e.preventDefault();
    send('cmd&TOGGLE');
    output("***TOGGLE Console Window***");
}, false);

document.getElementById('exit').addEventListener('touchstart', function (e) {
    e.preventDefault();
    var confirm = window.confirm("Do you want to release target?");
    if (confirm) {
        send('cmd&EXIT');
        output("***RELEASE TARGET***");
    }
}, false);

document.getElementById('lock').addEventListener('touchstart', function (e) {
    e.preventDefault();
    send('cmd&LOCK');
    output("***TOGGLE LOCK***");
}, false);

document.getElementById('leftclick').addEventListener('touchstart', function (e) {
    e.preventDefault();
    send('mc&left');
}, false);

document.getElementById('rightclick').addEventListener('touchstart', function (e) {
    e.preventDefault();
    send('mc&right');
}, false);
    document.getElementById('leftclick').addEventListener('touchend', function (e) {
    e.preventDefault();
    send('me&left');
}, false);

document.getElementById('rightclick').addEventListener('touchend', function (e) {
    e.preventDefault();
    send('me&right');
}, false);


document.getElementById('keyboard').onkeyup = function (e) {
    var key;
    switch (e.keyCode) {
        case 13:
            key = "{ENTER}";
            break;
        case 8:
            key = "{BACKSPACE}";
            break;
        case 27:
            key = "{ESC}";
            break;
        default :
            key = this.value;
    }
    this.value = "";

    send('ky&' + key);
};

function send(data) {
    if (ws !== null) {
        ws.send(data); //send method
    }
}

function output(data) {
    output_field.value += data + "\n";
    output_field.scrollTop = output_field.scrollHeight;
}

function startConn() {
    //start handshake
    ws.onopen = function (e) {
        console.log("connection established");
        output("connected!");
        
    }; //on open event

    ws.onclose = function (e) {
        output("disconnected!");
        console.log("connection close");
    }; //on close event

    ws.onmessage = function (msg) {
        if (msg.data !== undefined && msg.data !== '') {
            var data = msg.data.split('&');
            switch (data[0]) {
                case 'status':
                    toggleConn(lblConn, data[1]);
                    break;

                case 'cmd':
                case 'b':
                    output(data[1]);
                    break;
            }
        }
    };

    ws.onerror = function () {
        output("Connection Fail!");
        toggleConn(lblConn, 'OFF');
        ws = null;
    }; //on error event
}

function toggleConn(lbl, status) {
    if (status === 'ON') {
        lbl.style.color = "green";
    } else {
        lbl.style.color = "red";
    }
    lbl.textContent = status;
}

function setJoyPos(joy, x, y) {
    var dx = x - point.x;
    var dy = y - point.y;
    var dist = Math.sqrt(dx * dx + dy * dy);
    var rad = Math.atan2(dx, dy);
    joy.style.marginLeft = x + 'px';
    joy.style.marginTop = y + 'px';
    speed = Math.ceil(dist / 6);
    return rad;
}

var touch;
var angle = 0;
var speed = 0;
var point = {};

leftjoy.addEventListener('touchstart', function (e) {
    touchMove(e, this.firstChild);
    this.firstChild.style.display = 'block';
    centerP.style.display = 'block';
    touch = setInterval(function () {
        send("mm&" + angle + "&" + speed);
    }, 15);
}, false);

leftjoy.addEventListener('touchmove', touchMove, false);
leftjoy.addEventListener('touchend', function (e) {
    e.preventDefault();
    clearInterval(touch);
    this.firstChild.style.display = 'none';
    centerP.style.display = 'none';
}, false);

function touchMove(e, child) {
    e.preventDefault();
    var touch = e.changedTouches[0];
    var relX = touch.pageX - leftjoy.offsetLeft - 15;
    var relY = touch.pageY - leftjoy.offsetTop - 15;
    if (child) {
        point.x = relX;
        point.y = relY;
        centerP.style.marginLeft = point.x + "px";
        centerP.style.marginTop = point.y + "px";
    }
    angle = Math.round(setJoyPos(this.firstChild || child, relX, relY) * 180 / Math.PI - 90);
    //  absAng = Math.abs(angle);
    if (angle < 0) {
        angle += 360;
    }
}