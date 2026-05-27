const dgram = require('dgram');
const express = require('express');
const app = express();
const server = require('http').createServer(app);
const io = require('socket.io')(server);

// udp server (listens to unity)
const udpServer = dgram.createSocket('udp4');

udpServer.on('message', (msg) => {
    try {
        // parse unity data
        const telemetry = JSON.parse(msg.toString());
        // blast it to the web dashboard
        io.emit('telemetry', telemetry);
    } catch (e) {
        // ignore parse errors
    }
});

udpServer.bind(5005, () => {
    console.log('udp server listening for unity on port 5005');
});

// web server (serves the dashboard)
app.get('/', (req, res) => {
    res.sendFile(__dirname + '/index.html');
});

server.listen(3000, () => {
    console.log('dashboard running! open http://localhost:3000 in your browser');
});