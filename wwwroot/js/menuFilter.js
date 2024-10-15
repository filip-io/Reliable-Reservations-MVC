document.addEventListener('DOMContentLoaded', () => {
    const menuItems = Array.from(document.querySelectorAll('.menu-item'));
    const categoryGroups = Array.from(document.querySelectorAll('.category-group'));
    const categorySelect = document.getElementById('categorySelect');
    const minPriceSlider = document.getElementById('minPriceSlider');
    const maxPriceSlider = document.getElementById('maxPriceSlider');
    const minPriceValue = document.getElementById('minPriceValue');
    const maxPriceValue = document.getElementById('maxPriceValue');
    const filterButton = document.getElementById('filterButton');
    const menuItemsContainer = document.getElementById('menuItemsContainer');

    const parsePrice = (priceString) => {
        return parseFloat(priceString.replace(',', '.'));
    };

    const prices = menuItems.map(item => parsePrice(item.dataset.price));
    const minPrice = Math.floor(Math.min(...prices) * 100) / 100;
    const maxPrice = Math.ceil(Math.max(...prices) * 100) / 100;

    const initializePriceSliders = () => {
        [minPriceSlider, maxPriceSlider].forEach(slider => {
            slider.min = minPrice;
            slider.max = maxPrice;
            slider.step = 0.01;
        });
        minPriceSlider.value = minPrice;
        maxPriceSlider.value = maxPrice;
        updatePriceLabels();
    };

    const updatePriceLabels = () => {
        minPriceValue.textContent = parseFloat(minPriceSlider.value).toFixed(2);
        maxPriceValue.textContent = parseFloat(maxPriceSlider.value).toFixed(2);
    };

    const filterItems = () => {
        const selectedCategory = categorySelect.value;
        const minPrice = parseFloat(minPriceSlider.value);
        const maxPrice = parseFloat(maxPriceSlider.value);

        const filteredItems = menuItems.filter(item => {
            const itemCategory = item.closest('.category-group').dataset.category;
            const itemPrice = parsePrice(item.dataset.price);
            const categoryMatch = selectedCategory === '' || itemCategory === selectedCategory;
            const priceMatch = itemPrice >= minPrice && itemPrice < maxPrice;
            return categoryMatch && priceMatch;
        });

        menuItems.forEach(item => item.style.display = 'none');
        categoryGroups.forEach(group => group.style.display = 'none');

        filteredItems.forEach(item => {
            item.style.display = '';
            item.closest('.category-group').style.display = '';
        });

        if (filteredItems.length === 0) {
            menuItemsContainer.innerHTML = '<p class="text-center">No menu items match your filter criteria.</p>';
        } else {
            // Clear the "No results" message if items are found
            const noResultsMessage = menuItemsContainer.querySelector('p.text-center');
            if (noResultsMessage) {
                noResultsMessage.remove();
            }
        }
    };

    initializePriceSliders();

    [minPriceSlider, maxPriceSlider].forEach(slider =>
        slider.addEventListener('input', updatePriceLabels)
    );

    filterButton.addEventListener('click', filterItems);
});

const showDeleteModal = (menuItemId) => {
    document.getElementById('menuItemId').value = menuItemId;
    const deleteModal = new bootstrap.Modal(document.getElementById('deleteModal'));
    deleteModal.show();
};