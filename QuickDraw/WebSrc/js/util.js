export class Timer {
    timerID = null;
    startTime = null;
    duration = null;
    callback = null;
    elapsed= 0;
    paused = true;

    constructor(callback, duration) {
        this.callback = callback;
        this.duration = duration;
    }

    get timeElapsed() {
        if (this.paused) {
            return this.elapsed;
        }
        return (Date.now() - this.startTime) + this.elapsed;
    }

    get timeRemaining() {
        return this.duration - this.timeElapsed;
    }

    get timePercentElapsed() {
        return (this.timeElapsed / this.duration).toPrecision(3) * 100.0;
    }

    start() {
        this.startTime = Date.now();
        if (this.timerID != null) {
            clearTimeout(this.timerID);
        }
        this.timerID = setTimeout(() => {
            this.elapsed = this.duration;
            this.paused = true;
            this.callback();
        }, this.duration - this.elapsed);
        this.paused = false;
    }

    restart() {
        this.elapsed = 0;
        this.start();
    }

    pause() {
        if (!this.paused) {
            clearTimeout(this.timerID);
            this.elapsed += Date.now() - this.startTime;
            this.paused = true;
        }
    }

    resume() {
        this.start();
    }
}