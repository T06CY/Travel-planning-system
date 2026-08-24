document.addEventListener('DOMContentLoaded', () => {
    const form = document.querySelector('#hotelSearchForm');
    const results = document.querySelector('#hotelResults');
    if (!form || !results) return;

    // Build URL for current form values
    const url = () => {
        const p = new URLSearchParams();
        new FormData(form).forEach((v, k) => { if (v !== null && String(v).trim() !== '') p.append(k, v); });
        return p.toString() ? `${form.action}?${p}` : form.action;
    };

    // Fetch partial results and replace
    const load = async target => {
        results.classList.add('loading');
        try {
            const res = await fetch(target, { headers: { 'X-Requested-With': 'XMLHttpRequest' }, cache: 'no-store' });
            if (!res.ok) throw Error();
            results.innerHTML = await res.text();
            const total = results.querySelector('#hotelTotalCount')?.dataset.total;
            const label = document.querySelector('.results-count');
            if (label) label.textContent = `${total || 0} rooms found`;
            history.replaceState({}, '', target);
            bindPagination();
        } finally {
            results.classList.remove('loading');
        }
    };

    form.addEventListener('submit', e => { e.preventDefault(); load(url()); });

    // Auto-submit when filters change
    document.querySelectorAll('.auto-filter').forEach(e => e.addEventListener('change', () => load(url())));
    document.querySelector('#clearFilters')?.addEventListener('click', () => { form.reset(); load(form.action); });

    function bindPagination() {
        results.querySelectorAll('.activity-pagination a').forEach(a => a.addEventListener('click', e => { e.preventDefault(); load(a.href); }));
    }

    // Occupancy increment/decrement
    document.querySelectorAll('.occ-decrement').forEach(btn => btn.addEventListener('click', function () {
        const target = this.dataset.target;
        const input = form.querySelector(`[name='${target}']`);
        if (!input) return;
        const min = parseInt(input.getAttribute('min') || '0', 10);
        let val = parseInt(input.value || '0', 10);
        val = Math.max(min, val - 1);
        input.value = val;
        input.dispatchEvent(new Event('change', { bubbles: true }));
    }));

    // Reference-style occupancy dropdown and summary
    const occupancySummary = document.querySelector('.occupancy-summary');
    const occupancyMenu = document.querySelector('#occupancyMenu');
    const updateOccupancySummary = () => {
        const adults = form.querySelector("[name='Adults']")?.value || '2';
        const rooms = form.querySelector("[name='RequestedRooms']")?.value || '1';
        const children = form.querySelector("[name='Children']")?.value || '0';
        document.querySelector('#adultSummary').textContent = adults;
        document.querySelector('#roomSummary').textContent = rooms;
        document.querySelector('#roomSummary').nextSibling.textContent = Number(rooms) === 1 ? ' room' : ' rooms';
        document.querySelector('#roomValue').textContent = rooms;
        document.querySelector('#adultValue').textContent = adults;
        document.querySelector('#childrenValue').textContent = children;
    };
    if (occupancySummary && occupancyMenu) {
        occupancySummary.addEventListener('click', () => {
            const isOpen = occupancySummary.getAttribute('aria-expanded') === 'true';
            occupancySummary.setAttribute('aria-expanded', String(!isOpen));
            occupancyMenu.hidden = isOpen;
            occupancySummary.classList.toggle('open', !isOpen);
        });
        document.addEventListener('click', e => {
            if (!occupancyMenu.contains(e.target) && !occupancySummary.contains(e.target)) {
                occupancyMenu.hidden = true;
                occupancySummary.setAttribute('aria-expanded', 'false');
                occupancySummary.classList.remove('open');
            }
        });
        form.querySelectorAll("[name='Adults'], [name='Children'], [name='RequestedRooms']").forEach(input => input.addEventListener('input', updateOccupancySummary));
        document.querySelectorAll('.occ-increment, .occ-decrement').forEach(button => button.addEventListener('click', updateOccupancySummary));
        updateOccupancySummary();
    }
    document.querySelectorAll('.occ-increment').forEach(btn => btn.addEventListener('click', function () {
        const target = this.dataset.target;
        const input = form.querySelector(`[name='${target}']`);
        if (!input) return;
        const max = parseInt(input.getAttribute('max') || '999', 10);
        let val = parseInt(input.value || '0', 10);
        val = Math.min(max, val + 1);
        input.value = val;
        input.dispatchEvent(new Event('change', { bubbles: true }));
    }));

    // Price range + number sync and currency toggle
    const priceRange = document.querySelector('.price-range');
    const priceNumber = document.querySelector('.price-number');
    const currencyToggle = document.querySelector('.currency-toggle');
    const currencyInput = form.querySelector('input[name="Currency"]');
    const currencies = ['MYR', 'USD', 'SGD', 'EUR', 'GBP'];
    let currencyIndex = currencies.indexOf((currencyInput && currencyInput.value) || 'MYR');
    if (currencyIndex < 0) currencyIndex = 0;

    if (priceRange && priceNumber) {
        priceRange.addEventListener('input', function () { priceNumber.value = this.value; updatePriceSummary(); });
        priceNumber.addEventListener('input', function () { const v = parseFloat(this.value || '0'); if (!isNaN(v)) priceRange.value = Math.round(v); updatePriceSummary(); });
        function updatePriceSummary() {
            const summary = document.querySelector('#priceSummary');
            const triggerValue = document.querySelector('.price-filter-trigger-value');
            const value = (priceNumber.value && priceNumber.value.trim() !== '') ? priceNumber.value : '300';
            if (summary) summary.textContent = value;
            if (triggerValue) triggerValue.textContent = value;
            // Keep the slider synced with the numeric input
            if (priceRange) priceRange.value = Math.round(parseFloat(value) || 0);
        }
        updatePriceSummary();
    }
    if (currencyToggle) {
        currencyToggle.addEventListener('click', function () {
            currencyIndex = (currencyIndex + 1) % currencies.length;
            this.textContent = currencies[currencyIndex];
            if (currencyInput) currencyInput.value = currencies[currencyIndex];
            const currencySummary = document.querySelector('.price-filter-currency');
            if (currencySummary) currencySummary.textContent = currencies[currencyIndex];
        });
    }

    const priceTrigger = document.querySelector('.price-filter-trigger');
    const pricePopover = document.querySelector('.price-filter-popover');
    if (priceTrigger && pricePopover) {
        priceTrigger.addEventListener('click', () => {
            const open = priceTrigger.getAttribute('aria-expanded') === 'true';
            priceTrigger.setAttribute('aria-expanded', String(!open));
            pricePopover.classList.toggle('open', !open);
        });
    }

    bindPagination();
});
