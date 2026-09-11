(function () {
    "use strict";
    document.addEventListener("DOMContentLoaded", function () {
        document.querySelectorAll("[data-admin-seat-button]").forEach(function (button) {
            button.addEventListener("click", function () {
                if (button.disabled) return;
                const target = document.querySelector(button.dataset.seatTarget || "");
                if (!target) return;

                target.value = button.dataset.seatNumber || "";
                const map = button.closest(".admin-visual-seat-picker");
                if (map) {
                    const targetSelector = button.dataset.seatTarget;
                    map.querySelectorAll("[data-admin-seat-button]").forEach(function (seat) {
                        seat.classList.toggle("admin-seat-current", seat.dataset.seatTarget === targetSelector && seat === button);
                    });
                }
            });
        });
    });
}());
