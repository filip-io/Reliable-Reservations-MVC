// Modern JavaScript using ES6+ features
const initializeReservationForm = (data) => {
    // DOM element selection using object property shorthand
    const elements = {
        form: document.getElementById('reservationForm'),
        dateInput: document.getElementById('reservationDate'),
        guestsInput: document.getElementById('numberOfGuests'),
        timeSlotsSelect: document.getElementById('availableTimeSlots'),
        tablesDiv: document.getElementById('availableTables'),
        reserveButton: document.getElementById('reserveButton'),
        hiddenReservationDate: document.getElementById('hiddenReservationDate')
    };

    // Destructuring for cleaner access to data properties
    const { allTables, openingHours, closedDays, getReservationsUrl } = data;

    // State to store current reservations
    let currentReservations = [];

    // Initialize form
    const init = () => {
        updateDateInputConstraints();
        addEventListeners();
    };

    // Set up date input constraints
    const updateDateInputConstraints = () => {
        const today = new Date();
        const maxDate = new Date(today.getFullYear(), today.getMonth() + 3, today.getDate());

        elements.dateInput.min = formatDateForInput(today);
        elements.dateInput.max = formatDateForInput(maxDate);
    };

    // Helper function to format date for input
    const formatDateForInput = (date) => date.toISOString().split('T')[0];

    // Add event listeners
    const addEventListeners = () => {
        elements.dateInput.addEventListener('input', handleDateInput);
        elements.dateInput.addEventListener('change', updateAvailability);
        elements.guestsInput.addEventListener('change', updateAvailability);
        elements.timeSlotsSelect.addEventListener('change', handleTimeSlotChange);
        elements.form.addEventListener('submit', handleFormSubmit);
    };

    // Handle date input to check for closed days
    const handleDateInput = (event) => {
        const selectedDate = new Date(event.target.value);
        const dayOfWeek = selectedDate.toLocaleString('en-us', { weekday: 'long' });

        if (closedDays.includes(dayOfWeek)) {
            alert('The restaurant is closed on this day. Please select another date.');
            event.target.value = '';
        }
    };

    // Update availability based on date and guests
    const updateAvailability = () => {
        const { value: date } = elements.dateInput;
        const { value: numberOfGuests } = elements.guestsInput;

        if (date && numberOfGuests) {
            fetchReservations(date, numberOfGuests);
        }
    };

    // Fetch reservations from server
    const fetchReservations = async (date, numberOfGuests) => {
        try {
            const response = await axios.get(getReservationsUrl, { params: { date } });
            currentReservations = response.data;  // Store fetched reservations
            handleReservationsResponse(date, numberOfGuests);
        } catch (error) {
            handleReservationsError(error);
        }
    };

    // Handle successful reservations response
    const handleReservationsResponse = (date, numberOfGuests) => {
        const availableTimeSlots = getAvailableTimeSlots(date, numberOfGuests);
        populateAvailableTimeSlots(availableTimeSlots, date, numberOfGuests);
    };

    // Handle reservations fetch error
    const handleReservationsError = (error) => {
        console.error('Failed to fetch reservations:', error);
        elements.timeSlotsSelect.innerHTML = '<option value="">Failed to load available time slots.</option>';
        elements.tablesDiv.innerHTML = 'Failed to load tables.';
        elements.reserveButton.disabled = true;
    };

    // Get available time slots
    const getAvailableTimeSlots = (date, numberOfGuests) => {
        const selectedDate = new Date(date);
        const dayOfWeek = selectedDate.toLocaleString('en-us', { weekday: 'long' });
        const dayOpeningHours = openingHours.find(oh => oh.dayOfWeek === dayOfWeek);

        if (!dayOpeningHours || dayOpeningHours.isClosed) return [];

        const [openHour, openMinute] = dayOpeningHours.openTime.split(':').map(Number);
        const [closeHour, closeMinute] = dayOpeningHours.closeTime.split(':').map(Number);

        const openingMinutes = openHour * 60 + openMinute;
        const closingMinutes = closeHour * 60 + closeMinute;

        const numberOfSlots = Math.floor((closingMinutes - openingMinutes - 90) / 30) + 1;

        return Array.from({ length: numberOfSlots }, (_, index) => {
            const minutes = openingMinutes + index * 30;
            const hour = Math.floor(minutes / 60);
            const minute = minutes % 60;
            const timeString = `${hour.toString().padStart(2, '0')}:${minute.toString().padStart(2, '0')}`;

            return {
                value: timeString,
                display: timeString,
                available: getAvailableTables(date, timeString, numberOfGuests).length > 0
            };
        }).filter(time => time.available);
    };

    // Populate available time slots
    const populateAvailableTimeSlots = (availableTimes, date, numberOfGuests) => {
        elements.timeSlotsSelect.innerHTML = availableTimes.length
            ? `<option value="">Select a time slot</option>
               ${availableTimes.map(time => `<option value="${time.value}">${time.display}</option>`).join('')}`
            : '<option value="">No available time slots for the selected date</option>';

        elements.timeSlotsSelect.disabled = !availableTimes.length;
    };

    // Handle time slot change
    const handleTimeSlotChange = () => {
        const { value: date } = elements.dateInput;
        const { value: numberOfGuests } = elements.guestsInput;
        const { value: selectedTime } = elements.timeSlotsSelect;

        if (selectedTime) {
            const availableTables = getAvailableTables(date, selectedTime, numberOfGuests);
            populateAvailableTables(availableTables, numberOfGuests);
            updateReservationDateTime(date, selectedTime);
        }
    };

    // Get available tables
    const getAvailableTables = (date, selectedTime) => {
        const selectedDateTime = new Date(`${date}T${selectedTime}`);
        const occupiedTableIds = currentReservations
            .filter(reservation => Math.abs(new Date(reservation.reservationDate) - selectedDateTime) < 2 * 60 * 60 * 1000)
            .flatMap(reservation => reservation.tables.map(table => table.tableId));

        return allTables.filter(table => !occupiedTableIds.includes(table.tableId));
    };

    // Populate available tables
    const populateAvailableTables = (tables, numberOfGuests) => {
        if (tables.length) {
            elements.tablesDiv.innerHTML = tables.map(table => `
            <div>
                <input type="checkbox" name="TableNumbers" value="${table.tableNumber}" id="table-${table.tableId}">
                <label for="table-${table.tableId}">Table ${table.tableNumber} (${table.seatingCapacity} seats, ${table.location})</label>
            </div>
        `).join('');

            elements.tablesDiv.querySelectorAll('input[type="checkbox"]').forEach(checkbox => {
                checkbox.addEventListener('change', () => checkIfEnoughTablesSelected(tables, numberOfGuests));
            });

            // Initial check to set button state
            checkIfEnoughTablesSelected(tables, numberOfGuests);
        } else {
            elements.tablesDiv.textContent = 'No available tables for the selected time.';
            elements.reserveButton.disabled = true;
        }
    };

    // Check if enough tables are selected
    const checkIfEnoughTablesSelected = (tables, numberOfGuests) => {
        const selectedTables = Array.from(elements.tablesDiv.querySelectorAll('input[type="checkbox"]:checked'))
            .map(checkbox => tables.find(table => table.tableNumber === parseInt(checkbox.value)));

        const totalSeats = selectedTables.reduce((sum, table) => sum + table.seatingCapacity, 0);
        elements.reserveButton.disabled = totalSeats < numberOfGuests;

        // Remove any existing feedback
        const existingFeedback = elements.tablesDiv.querySelector('.table-selection-feedback');
        if (existingFeedback) existingFeedback.remove();

        // Provide feedback to the user
        const feedbackMessage = totalSeats < numberOfGuests
            ? `Please select more tables. Current capacity: ${totalSeats}/${numberOfGuests} needed.`
            : `Sufficient seating selected: ${totalSeats}/${numberOfGuests} seats.`;

        elements.tablesDiv.insertAdjacentHTML('beforeend', `
        <div class="table-selection-feedback" style="margin-top: 10px; margin-bottom: 0;">
            <hr style="margin-bottom: 10px;">
            <p style="margin: 0;">${feedbackMessage}</p>
        </div>
    `);
    };

    // Update reservation date and time
    const updateReservationDateTime = (date, time) => {
        const reservationDateTime = new Date(`${date}T${time}`);
        elements.hiddenReservationDate.value = reservationDateTime.toLocaleString();
    };

    // Handle form submission
    const handleFormSubmit = (event) => {
        event.preventDefault();
        if (elements.reserveButton.disabled) {
            alert('Please ensure all fields are filled correctly and sufficient tables are selected.');
            return;
        }
        // Log form data for debugging
        const formData = new FormData(elements.form);
        for (let [key, value] of formData.entries()) {
            console.log(`${key}: ${value}`);
        }
        elements.form.submit();
    };

    // Initialize the form
    init();
};