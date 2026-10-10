"use strict";

document.addEventListener("DOMContentLoaded", function () {
    function hidePreloader() {
        var preloader = document.querySelector(".spinner-wrapper");
        preloader.classList.add("opacity-0");
        preloader.style.position = "static";
    }

    setTimeout(function () {
        hidePreloader();
    }, 2000);
});
