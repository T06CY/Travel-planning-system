document.addEventListener('DOMContentLoaded', () => {

    const select = document.getElementById('participantCount');
    const total = document.getElementById('bookingTotal');
    const text = document.getElementById('participantText');

    if (!select || !total)
        return;

    const update = () => {

        const count = Number(select.value || 1);

        text.textContent = count;

        total.textContent =
            `RM ${(Number(window.activityPrice) * count).toFixed(2)}`;
    };

    select.addEventListener('change', update);

    update();

});