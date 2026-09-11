(function () {
    "use strict";
    document.addEventListener("DOMContentLoaded", function () {
        const outboundInputs = Array.from(document.querySelectorAll(".seat-input"));
        const returnInputs = Array.from(document.querySelectorAll(".return-seat-input"));
        const passengers = Array.from(document.querySelectorAll(".seat-passenger-button"));
        const legButtons = Array.from(document.querySelectorAll(".seat-leg-button"));
        const picker = document.getElementById("seatPicker");
        const message = document.querySelector("[data-seat-picker-message]");
        const seatError = document.querySelector("[data-seat-validation-error]");
        const form = document.querySelector("form");
        const phoneCountry = document.getElementById("phoneCountry");
        const phoneHint = document.getElementById("phoneFormatHint");
        let activePassenger = 0;
        let activeLeg = "outbound";
        if (!outboundInputs.length || !picker) return;
        if (phoneCountry && phoneHint) {
            phoneCountry.addEventListener("change", function () {
                phoneHint.textContent = phoneCountry.value === "MY"
                    ? "Malaysia: 10–11 digits, for example 0123456789."
                    : "International format: include +country code, for example +6581234567.";
            });
        }
        const returnTrip = returnInputs.length > 0;
        const maps = Array.from(picker.querySelectorAll("[data-seat-leg-map]"));
        if (returnTrip && maps.length === 1) {
            const returnMap = maps[0].cloneNode(true);
            returnMap.dataset.seatLegMap = "return";
            const front = returnMap.querySelector(".simple-aircraft-front");
            if (front) front.textContent = "RETURN FLIGHT";
            maps[0].after(returnMap);
            maps.push(returnMap);
        }
        function inputsFor(leg) { return leg === "return" ? returnInputs : outboundInputs; }
        function inputFor(leg, index) { return inputsFor(leg).find(function (input) { return Number(input.dataset.passengerIndex) === index; }); }
        function cabinFor(leg) { return leg === "return" ? picker.dataset.returnCabin : picker.dataset.outboundCabin; }
        function refresh() {
            const selected = inputsFor(activeLeg).map(function (input) { return (input.value || "").trim().toUpperCase(); });
            const activeCabin = cabinFor(activeLeg) || "Economy";
            maps.forEach(function (map) {
                const visible = map.dataset.seatLegMap === activeLeg;
                map.hidden = !visible;
                if (!visible) return;
                map.querySelectorAll(".visual-seat").forEach(function (seat) {
                    const number = (seat.dataset.seatNumber || "").toUpperCase();
                    const mine = selected[activePassenger] === number;
                    const other = selected.some(function (value, index) { return index !== activePassenger && value === number && value !== ""; });
                    const allowedCabin = seat.dataset.seatCabin === activeCabin;
                    seat.classList.toggle("seat-selected", mine);
                    seat.classList.toggle("seat-taken-by-other", other);
                    seat.classList.toggle("seat-unavailable-cabin", !allowedCabin);
                    seat.disabled = seat.classList.contains("seat-occupied") || other || !allowedCabin;
                });
            });
            passengers.forEach(function (button) {
                const index = Number(button.dataset.passengerIndex);
                const out = (inputFor("outbound", index)?.value || "").trim().toUpperCase();
                const back = (inputFor("return", index)?.value || "").trim().toUpperCase();
                button.classList.toggle("active", index === activePassenger);
                button.classList.toggle("complete", Boolean(out) && (!returnTrip || Boolean(back)));
                const status = button.querySelector("small");
                if (status) status.textContent = returnTrip ? (out || "-") + " / " + (back || "-") : (out || "Not selected");
            });
            legButtons.forEach(function (button) { button.classList.toggle("active", button.dataset.seatLeg === activeLeg); });
            const current = selected[activePassenger] || "";
            const currentSeat = current ? maps.find(function (map) { return map.dataset.seatLegMap === activeLeg; })?.querySelector('[data-seat-number="' + current + '"]') : null;
            const seatType = currentSeat?.dataset.seatType || "";
            if (message) message.textContent = current
                ? "Passenger " + (activePassenger + 1) + " selected " + (activeLeg === "return" ? "return " : "") + "seat " + current + (seatType ? " (" + seatType + ")" : "") + " in " + activeCabin + "."
                : "Passenger " + (activePassenger + 1) + " must select a " + activeCabin + " seat for the " + (activeLeg === "return" ? "return" : "outbound") + " flight.";
        }
        passengers.forEach(function (button) { button.addEventListener("click", function () { activePassenger = Number(button.dataset.passengerIndex); refresh(); }); });
        legButtons.forEach(function (button) { button.addEventListener("click", function () { activeLeg = button.dataset.seatLeg; refresh(); }); });
        picker.querySelectorAll("[data-seat-leg-map] .visual-seat").forEach(function (seat) { seat.addEventListener("click", function () { if (seat.disabled) return; const input = inputFor(activeLeg, activePassenger); if (input) { input.value = seat.dataset.seatNumber || ""; refresh(); } }); });
        if (form) form.addEventListener("submit", function (event) {
            const placeholderFields = Array.from(form.querySelectorAll("input[name$='.FirstName'], input[name$='.LastName'], input[name$='.PassportNumber']"));
            const blocked = ["xxx", "test", "testing", "abc", "sample", "none", "n/a", "na", "unknown"];
            const invalidPlaceholder = placeholderFields.find(function (field) {
                const value = (field.value || "").trim().toLowerCase();
                return blocked.includes(value) || (value.length > 1 && value.split("").every(function (character) { return character === value[0]; }));
            });
            if (invalidPlaceholder) {
                event.preventDefault();
                window.alert("Please enter real passenger details. Placeholder values such as xxx or test are not accepted.");
                invalidPlaceholder.focus();
                return;
            }
            const missingOut = outboundInputs.findIndex(function (input) { return !(input.value || "").trim(); });
            const missingBack = returnTrip ? returnInputs.findIndex(function (input) { return !(input.value || "").trim(); }) : -1;
            if (missingOut < 0 && missingBack < 0) { if (seatError) seatError.textContent = ""; return; }
            event.preventDefault(); activeLeg = missingOut >= 0 ? "outbound" : "return"; activePassenger = missingOut >= 0 ? missingOut : missingBack; refresh();
            const missingText = activeLeg === "return" ? "return seat" : "outbound seat";
            if (seatError) seatError.textContent = "Passenger " + (activePassenger + 1) + " must select a " + missingText + ".";
            window.alert("Please select " + missingText + " for Passenger " + (activePassenger + 1) + " before confirming.");
            if (message) message.textContent = "Please select both seats for every passenger before confirming.";
            picker.scrollIntoView({ behavior: "smooth", block: "center" });
        });
        outboundInputs.concat(returnInputs).forEach(function (input) { input.addEventListener("change", refresh); });
        refresh();
    });
})();
