document.addEventListener("DOMContentLoaded", function () {

    const form =
        document.getElementById("activitySearchForm");

    const results =
        document.getElementById("activityResults");

    const clearButton =
        document.getElementById("clearFilters");


    if (!form || !results) {
        return;
    }



    async function loadActivities(
        url,
        updateHistory = true
    ) {

        try {

            results.classList.add("loading");


            const response = await fetch(
                url,
                {
                    method: "GET",

                    headers: {
                        "X-Requested-With": "XMLHttpRequest"
                    },

                    cache: "no-store"
                }
            );


            if (!response.ok) {

                throw new Error(
                    "Unable to load activities."
                );

            }


            const html =
                await response.text();


            // Replace activity result cards
            results.innerHTML =
                html;


            results.classList.remove(
                "loading"
            );



            const totalElement =
                results.querySelector(
                    "#activityTotalCount"
                );

            const resultsCount =
                document.querySelector(
                    ".results-count"
                );


            if (
                totalElement &&
                resultsCount
            ) {

                const total =
                    totalElement.getAttribute(
                        "data-total"
                    );


                resultsCount.textContent =
                    `${total} activities found`;

            }


            // Re-bind pagination
            // because AJAX replaced the old HTML
            bindPagination();




            if (updateHistory) {

                window.history.replaceState(
                    {
                        activityPage: true
                    },
                    "",
                    url
                );

            }

        }
        catch (error) {

            console.error(error);

            results.classList.remove(
                "loading"
            );

        }

    }



    function getSearchUrl() {

        const formData =
            new FormData(form);


        const parameters =
            new URLSearchParams();


        for (const [key, value]
            of formData.entries()) {


            // Ignore ASP.NET internal fields
            if (
                key.startsWith("__Invariant")
            ) {

                continue;

            }


            // Ignore empty values
            if (
                value !== null &&
                value.toString().trim() !== ""
            ) {

                parameters.append(
                    key,
                    value
                );

            }

        }


        const query =
            parameters.toString();


        return query
            ? `${form.action}?${query}`
            : form.action;

    }



    form.addEventListener(
        "submit",
        function (event) {

            event.preventDefault();


            const url =
                getSearchUrl();


            loadActivities(
                url
            );

        }
    );



    document
        .querySelectorAll(
            ".auto-filter"
        )
        .forEach(
            function (element) {

                element.addEventListener(
                    "change",
                    function () {

                        loadActivities(
                            getSearchUrl()
                        );

                    }
                );

            }
        );


    if (clearButton) {

        clearButton.addEventListener(
            "click",
            function () {


                // Use document.querySelector because
                // Category, MinPrice, MaxPrice and Sort
                // are outside the physical form.

                const search =
                    document.querySelector(
                        '[name="Search"]'
                    );


                const destination =
                    document.querySelector(
                        '[name="Destination"]'
                    );


                const date =
                    document.querySelector(
                        '[name="Date"]'
                    );


                const category =
                    document.querySelector(
                        '[name="CategoryId"]'
                    );


                const minPrice =
                    document.querySelector(
                        '[name="MinPrice"]'
                    );


                const maxPrice =
                    document.querySelector(
                        '[name="MaxPrice"]'
                    );


                const sort =
                    document.querySelector(
                        '[name="Sort"]'
                    );


                // Clear search
                if (search) {

                    search.value = "";

                }


                // Reset destination
                if (destination) {

                    destination.value = "";

                }


                // Clear activity date
                if (date) {

                    date.value = "";

                }


                // Reset category
                if (category) {

                    category.value = "";

                }


                // Clear minimum price
                if (minPrice) {

                    minPrice.value = "";

                }


                // Clear maximum price
                if (maxPrice) {

                    maxPrice.value = "";

                }


                // Reset sort to Recommended
                if (sort) {

                    sort.value = "featured";

                }


                // Reload all activities
                loadActivities(
                    form.action
                );

            }
        );

    }


    function bindPagination() {

        const paginationLinks =
            results.querySelectorAll(
                ".activity-pagination .page-button"
            );


        paginationLinks.forEach(
            function (link) {


                // Current page is a span
                // and should not be clickable
                if (
                    link.classList.contains(
                        "active"
                    )
                ) {

                    return;

                }


                link.addEventListener(
                    "click",
                    function (event) {

                        event.preventDefault();


                        const url =
                            link.getAttribute(
                                "href"
                            );


                        if (!url) {

                            return;

                        }


                        // Load selected page
                        loadActivities(
                            url
                        );


                        // Scroll back to activity section
                        const section =
                            document.querySelector(
                                ".activity-section"
                            );


                        if (section) {

                            window.scrollTo({
                                top:
                                    section.offsetTop - 80,

                                behavior:
                                    "smooth"
                            });

                        }

                    }
                );

            }
        );

    }


    // Bind pagination when page first loads
    bindPagination();



    window.addEventListener(
        "popstate",
        function () {

            // Full reload prevents browser
            // from showing only the AJAX partial view
            window.location.reload();

        }
    );

});