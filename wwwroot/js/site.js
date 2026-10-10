document.addEventListener("DOMContentLoaded", function () {

    const topbar = document.querySelector(".app-topbar");

    if (!topbar) {
        return;
    }

    function updateTopbar() {
        topbar.classList.toggle(
            "is-scrolled",
            window.scrollY > 8
        );
    }

    updateTopbar();

    window.addEventListener(
        "scroll",
        updateTopbar,
        { passive: true }
    );

});