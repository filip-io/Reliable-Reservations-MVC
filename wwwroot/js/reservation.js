const initializeReservationForm = (data) => {
    const elements = {
        form: document.getElementById('reservationForm'),
        dateInput: document.getElementById('reservationDate'),
        guestsInput: document.getElementById('numberOfGuests'),
        timeSlotsSelect: document.getElementById('availableTimeSlots'),
        tablesDiv: document.getElementById('availableTables'),
        reserveButton: document.getElementById('reserveButton') || document.getElementById('updateButton'),
        hiddenReservationDate: document.getElementById('hiddenReservationDate'),
        specialRequestsInput: document.getElementById('SpecialRequests')
    };

    const { allTables, openingHours, closedDays, getReservationsUrl, isEditMode, existingReservation } = data;

    let currentReservations = [];
    let flatpickrInstance;
    let currentReservationTables = []; // New property to store current reservation's tables

    const init = () => {
        if (isEditMode && existingReservation) {
            currentReservationTables = existingReservation.tableNumbers;
        }
        initializeFlatpickr();
        addEventListeners();
        if (isEditMode && existingReservation) {
            prefillFormData(existingReservation);
        }
    };

    const initializeFlatpickr = () => {
        const today = new Date();
        const maxDate = new Date(today.getFullYear(), today.getMonth() + 3, today.getDate());

        flatpickrInstance = flatpickr(elements.dateInput, {
            dateFormat: "Y-m-d",
            minDate: "today",
            maxDate: maxDate,
            disable: [
                function (date) {
                    return closedDays.includes(date.toLocaleString('en-us', { weekday: 'long' }));
                }
            ],
            onChange: (selectedDates, dateStr) => {
                handleDateInput(dateStr);
                updateAvailability();
            }
        });
    };

    const addEventListeners = () => {
        elements.guestsInput.addEventListener('change', updateAvailability);
        elements.timeSlotsSelect.addEventListener('change', handleTimeSlotChange);
        elements.form.addEventListener('submit', handleFormSubmit);
    };

    const prefillFormData = (reservation) => {
        const reservationDate = new Date(reservation.reservationDate);
        flatpickrInstance.setDate(reservationDate, true);

        elements.guestsInput.value = reservation.numberOfGuests;

        const timeSlot = reservationDate.toLocaleTimeString('en-US', { hour: '2-digit', minute: '2-digit', hour12: false });

        updateAvailability();
        setTimeout(() => {
            elements.timeSlotsSelect.value = timeSlot;
            handleTimeSlotChange();

            // Move table pre-selection here to ensure available tables are populated
            setTimeout(() => {
                preSelectTables(reservation.tableNumbers);
            }, 500); // Add a delay to ensure tables are populated
        }, 500);

        if (elements.specialRequestsInput) {
            elements.specialRequestsInput.value = reservation.specialRequests || '';
        }

        updateReservationDateTime(flatpickrInstance.selectedDates[0], timeSlot);
    };

    const preSelectTables = (tableNumbers) => {
        if (!tableNumbers || !Array.isArray(tableNumbers)) {
            console.error('Invalid tableNumbers:', tableNumbers);
            return;
        }

        console.log('Table numbers to select:', tableNumbers);
        elements.tablesDiv.querySelectorAll('input[type="checkbox"]').forEach(checkbox => {
            console.log('Checking table:', checkbox.value);
            if (tableNumbers.includes(parseInt(checkbox.value))) {
                console.log('Selecting table:', checkbox.value);
                checkbox.checked = true;
            }
        });
        checkIfEnoughTablesSelected(allTables, elements.guestsInput.value);
    };

    const handleDateInput = (dateStr) => {
        if (dateStr) {
            elements.hiddenReservationDate.value = dateStr;
        }
    };

    const updateAvailability = () => {
        const date = elements.dateInput.value;
        const numberOfGuests = elements.guestsInput.value;

        if (date && numberOfGuests) {
            fetchReservations(date, numberOfGuests);
        }
    };

    const fetchReservations = async (date, numberOfGuests) => {
        try {
            const response = await axios.get(getReservationsUrl, { params: { date } });
            currentReservations = response.data;
            handleReservationsResponse(date, numberOfGuests);
        } catch (error) {
            handleReservationsError(error);
        }
    };

    const handleReservationsResponse = (date, numberOfGuests) => {
        const availableTimeSlots = getAvailableTimeSlots(date, numberOfGuests);
        populateAvailableTimeSlots(availableTimeSlots, date, numberOfGuests);
    };

    const handleReservationsError = (error) => {
        console.error('Failed to fetch reservations:', error);
        elements.timeSlotsSelect.innerHTML = '<option value="">Failed to load available time slots.</option>';
        elements.tablesDiv.innerHTML = 'Failed to load tables.';
        elements.reserveButton.disabled = true;
    };

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

    const populateAvailableTimeSlots = (availableTimes, date, numberOfGuests) => {
        elements.timeSlotsSelect.innerHTML = availableTimes.length
            ? `<option value="">Select a time slot</option>
               ${availableTimes.map(time => `<option value="${time.value}">${time.display}</option>`).join('')}`
            : '<option value="">No available time slots for the selected date</option>';

        elements.timeSlotsSelect.disabled = !availableTimes.length;
    };

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

    const getAvailableTables = (date, selectedTime) => {
        const selectedDateTime = new Date(`${date}T${selectedTime}`);
        const occupiedTableIds = currentReservations
            .filter(reservation => {
                // Exclude the current reservation being edited from the filter
                return reservation.reservationId !== (existingReservation ? existingReservation.reservationId : null) &&
                    Math.abs(new Date(reservation.reservationDate) - selectedDateTime) < 2 * 60 * 60 * 1000;
            })
            .flatMap(reservation => reservation.tables.map(table => table.tableId));

        return allTables.filter(table =>
            !occupiedTableIds.includes(table.tableId) || currentReservationTables.includes(table.tableNumber)
        );
    };

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

            checkIfEnoughTablesSelected(tables, numberOfGuests);
        } else {
            elements.tablesDiv.textContent = 'No available tables for the selected time.';
            elements.reserveButton.disabled = true;
        }
    };

    const checkIfEnoughTablesSelected = (tables, numberOfGuests) => {
        const selectedTables = Array.from(elements.tablesDiv.querySelectorAll('input[type="checkbox"]:checked'))
            .map(checkbox => tables.find(table => table.tableNumber === parseInt(checkbox.value)));

        const totalSeats = selectedTables.reduce((sum, table) => sum + table.seatingCapacity, 0);
        elements.reserveButton.disabled = totalSeats < numberOfGuests;

        const existingFeedback = elements.tablesDiv.querySelector('.table-selection-feedback');
        if (existingFeedback) existingFeedback.remove();

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

    const updateReservationDateTime = (date, time) => {
        const reservationDateTime = new Date(`${date}T${time}`);
        elements.hiddenReservationDate.value = reservationDateTime.toLocaleString();
    };

    const handleFormSubmit = (event) => {
        event.preventDefault();
        if (elements.reserveButton.disabled) {
            alert('Please ensure all fields are filled correctly and sufficient tables are selected.');
            return;
        }
        const formData = new FormData(elements.form);
        for (let [key, value] of formData.entries()) {
            console.log(`${key}: ${value}`);
        }
        elements.form.submit();
    };

    init();
};