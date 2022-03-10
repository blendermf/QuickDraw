if (window.QuickDrawWindows === true) {
    document.body.classList.add("QuickDrawWindows");
}

if (window.QuickDrawMacOs === true) {
    document.body.classList.add("QuickDrawMacOs");
}

function PostMessage(message) {
    if (window.QuickDrawWindows === true) {
        window.chrome.webview.postMessage(message)
    } else if (window.QuickDrawMacOs === true) {
        window.webkit.messageHandlers.bridge.postMessage(message);
    }
}

class Timer {
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

const resolver = Promise.resolve({resource_response: { data: {}}});

class Slideshow {
    imagePreload = new Image();
    pins = [];
    currentPinNum = 0;
    currentTimer = null;
    currentInterval = null;
    pinClickEvent = e => {
        PostMessage({
            type: "openImage",
            path: this.pins[this.currentPinNum]
        });
    };
    init() {
        const pauseButton = document.getElementById("pause");
        const continueButton = document.getElementById("continue");
        const previousButton = document.getElementById("previous");
        const skipButton = document.getElementById("skip");
        const stopButton = document.getElementById("stop");
        const grayscaleButton = document.getElementById("grayscale");
        const slideshow = document.getElementById('slideshow');
        const slideshow_image = document.getElementById('slideshow-image');
        const progress = document.getElementById("slideshow-progress");
        const progress_bar = document.getElementById("slideshow-progress-bar");

        pauseButton.onclick = () => {
            this.currentTimer.pause();
            pauseButton.disabled = true;
            continueButton.disabled = false;
        };

        continueButton.onclick = () => {
            this.currentTimer.resume();
            continueButton.disabled = true;
            pauseButton.disabled = false;
        };

        previousButton.onclick = () => {
            if (slideshowData.interval !== 0) {
                this.currentTimer.pause();
            }

            this.changeImage(true);
        };

        skipButton.onclick = () => {
            if (slideshowData.interval !== 0) {
                this.currentTimer.pause();
            }

            this.changeImage();
        };

        stopButton.onclick = () => {
            if (window.QuickDrawWindows === true) {
                window.location.href="/index.html";
            } else if (window.QuickDrawMacOs === true) {
                PostMessage({
                    type: "stopSlideshow"
                });
            }
        };

        grayscaleButton.onclick = () => {
            slideshow_image.classList.toggle("grayscale");
        }
    }

    startSlideshow() {
        if (slideshowData.images != undefined && slideshowData.images.length > 0) {
            const slideshow = document.getElementById('slideshow');
            const slideshow_image = document.getElementById('slideshow-image');
            const progress = document.getElementById("slideshow-progress");
            const progress_bar = document.getElementById("slideshow-progress-bar");
            const pauseButton = document.getElementById("pause");

            this.pins = shuffle(slideshowData.images);
            this.currentPinNum = 0;

            if (this.pins.length > 1) {
                this.preloadImage(this.pins[1]);
            }

            slideshow_image.src = this.pins[0];
            slideshow_image.removeEventListener('click', this.pinClickEvent);
            slideshow_image.addEventListener('click', this.pinClickEvent);
            slideshow.classList.add('visible');
            slideshow_image.classList.add("visible");
            document.body.classList.add('noScroll');
            document.body.classList.add('noScroll');

            if (slideshowData.interval !== 0) {
                pauseButton.disabled = false;
                //setTimeout((()=> {
                    this.currentTimer = new Timer(this.changeImage.bind(this), slideshowData.interval);
                    progress.style.width="0%";
                    progress_bar.style.display = "block";
                    this.currentTimer.start();
                    this.resetInterval();
                //}).bind(this), 2000);
            }
        } else {
            // No images, respond
        }
    }
    
    async preloadImage(pin) {
        this.imagePreload.src = pin;
    }

    changeImage(back = false) {

        if (back) {
            this.currentPinNum--;
            if (this.currentPinNum < 0) {
                this.currentPinNum = this.pins.length - 1;
                this.preloadImage(this.pins[0]);
            }
        } else {
            this.currentPinNum++;
            if (this.currentPinNum >= this.pins.length) {
                this.currentPinNum = 0;
                this.preloadImage(this.pins[1]);
            }
        }

        const pauseButton = document.getElementById("pause");
        const continueButton = document.getElementById("continue");
        const previousButton = document.getElementById("previous");
        const skipButton = document.getElementById("skip")
        const slideshow_image = document.getElementById('slideshow-image');
        const progress = document.getElementById("slideshow-progress");
        const progress_bar = document.getElementById("slideshow-progress-bar");

        slideshow_image.classList.toggle("visible");
        progress_bar.style.display = "none";
        pauseButton.disabled = true;
        continueButton.disabled = true;
        previousButton.disabled = true;
        skipButton.disabled = true;
    
        // setTimeout((() => {
            if (this.currentInterval !== null) {
                clearInterval(this.currentInterval);
            }
    

            slideshow_image.src = this.pins[this.currentPinNum];
            
            slideshow_image.removeEventListener('click', this.pinClickEvent);
            slideshow_image.addEventListener('click', this.pinClickEvent);
            slideshow_image.classList.toggle("visible");
    
            // setTimeout((()=> {
                previousButton.disabled = false;
                skipButton.disabled = false;
                if (this.currentTimer !== null && slideshowData.interval !== 0) {
                    pauseButton.disabled = false;
                    progress.style.width="0%";
                    progress_bar.style.display = "block";
                    this.currentTimer.restart();
                    this.resetInterval();
                }
            // }).bind(this), 2000);
        // }).bind(this), 1000);
    }

    resetInterval() {
        if (this.currentInterval !== null) {
            clearInterval(this.currentInterval);
            this.currentInterval = null;
        }
        let progress = document.getElementById("slideshow-progress");
        this.currentInterval = setInterval((() => {
            progress.style.width = `${this.currentTimer.timePercentElapsed}%`;
        }).bind(this), 500);
    }
}

function shuffle(array) {
    let m = array.length, t, i;

    while (m) {

        // Pick a remaining element…
        i = Math.floor(secureMathRandom() * m--);
    
        // And swap it with the current element.
        t = array[m];
        array[m] = array[i];
        array[i] = t;
      }
    
      return array;
}

function secureMathRandom() {
    // Divide a random UInt32 by the maximum value (2^32 -1) to get a result between 0 and 1
    return window.crypto.getRandomValues(new Uint32Array(1))[0] / 4294967295;
}

var slideshow = new Slideshow();
slideshow.init();
slideshow.startSlideshow();
