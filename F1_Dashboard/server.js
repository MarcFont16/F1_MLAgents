const dgram = require('dgram');
const express = require('express');
const app = express();
const server = require('http').createServer(app);
const io = require('socket.io')(server);

// udp server (listens to unity)
const udpServer = dgram.createSocket('udp4');

udpServer.on('message', (msg) => {
    try {
        const telemetry = JSON.parse(msg.toString());
        io.emit('telemetry', telemetry);
    } catch (e) {
        console.log("parse error:", msg.toString());
    }
});

udpServer.bind(5005, '0.0.0.0', () => {
    console.log('udp server listening on all interfaces (port 5005)');
});

// web server (serves the dashboard)
app.get('/', (req, res) => {
    res.sendFile(__dirname + '/index.html');
});

server.listen(3000, () => {
    console.log('dashboard running! open http://localhost:3000 in your browser');
});