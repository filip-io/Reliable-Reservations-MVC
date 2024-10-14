function initializeReservationForm(urls) {
    const form = document.getElementById('reservationForm');
    const dateInput = document.getElementById('reservationDate');
    const guestsInput = document.getElementById('numberOfGuests');
    const timeSlotsSelect = document.getElementById('availableTimeSlots');
    const tablesDiv = document.getElementById('availableTables');
    const reserveButton = document.getElementById('reserveButton');

    let allTables = [];
    let openingHours = [];

    // Fetch opening hours when the page loads
    fetchOpeningHours();

    [dateInput, guestsInput].forEach(input => {
        input.addEventListener('change', function () {
            const date = dateInput.value;
            const numberOfGuests = guestsInput.value;

            if (date && numberOfGuests) {
                fetchReservationsAndTables(date, numberOfGuests);
            }
        });
    });

    function fetchOpeningHours() {
        axios.get(urls.getOpeningHoursUrl)
            .then(function (response) {
                openingHours = response.data;
                updateDateInputConstraints();
            })
            .catch(function (error) {
                console.error('Failed to fetch opening hours:', error);
            });
    }

    function updateDateInputConstraints() {
        const today = new Date();
        const maxDate = new Date();
        maxDate.setMonth(maxDate.getMonth() + 3); // Allow bookings up to 3 months in advance

        dateInput.min = today.toISOString().split('T')[0];
        dateInput.max = maxDate.toISOString().split('T')[0];

        // Disable closed days
        dateInput.addEventListener('input', function () {
            const selectedDate = new Date(this.value);
            const dayOfWeek = selectedDate.toLocaleString('en-us', { weekday: 'long' });
            const isClosedDay = openingHours.find(oh => oh.dayOfWeek === dayOfWeek)?.isClosed;

            if (isClosedDay) {
                alert('The restaurant is closed on this day. Please select another date.');
                this.value = '';
            }
        });
    }

    function fetchReservationsAndTables(date, numberOfGuests) {
        Promise.all([
            axios.get(urls.getReservationsUrl, { params: { date: date } }),
            axios.get(urls.getAllTablesUrl)
        ])
            .then(function ([reservationsResponse, tablesResponse]) {
                allTables = tablesResponse.data;
                const reservations = reservationsResponse.data;
                const availableTimeSlots = getAvailableTimeSlotsForDate(date, reservations, numberOfGuests);
                populateAvailableTimeSlots(availableTimeSlots, numberOfGuests, date, reservations);
            })
            .catch(function (error) {
                console.error('Axios request failed:', error);
                timeSlotsSelect.innerHTML = '<option value="">Failed to load available time slots.</option>';
                tablesDiv.innerHTML = 'Failed to load tables.';
                reserveButton.disabled = true;
            });
    }

    function getAvailableTimeSlotsForDate(date, reservations, numberOfGuests) {
        const selectedDate = new Date(date);
        const dayOfWeek = selectedDate.toLocaleString('en-us', { weekday: 'long' });
        const dayOpeningHours = openingHours.find(oh => oh.dayOfWeek === dayOfWeek);

        if (!dayOpeningHours || dayOpeningHours.isClosed) {
            return [];
        }

        const [openHour, openMinute] = dayOpeningHours.openTime.split(':').map(Number);
        const [closeHour, closeMinute] = dayOpeningHours.closeTime.split(':').map(Number);

        let availableTimes = [];
        const interval = 30; // 30 minutes interval

        // Convert opening and closing times to minutes since midnight for easier comparison
        const openingMinutes = openHour * 60 + openMinute;
        const closingMinutes = closeHour * 60 + closeMinute;

        for (let minutes = openingMinutes; minutes < closingMinutes - 120; minutes += interval) {
            const hour = Math.floor(minutes / 60);
            const minute = minutes % 60;

            const timeString = `${hour.toString().padStart(2, '0')}:${minute.toString().padStart(2, '0')}`;

            // Check if there are available tables for this time slot
            const availableTables = getAvailableTables(date, timeString, numberOfGuests, reservations);
            if (availableTables.length > 0) {
                availableTimes.push({
                    value: timeString,
                    display: timeString,
                    available: true
                });
            }
        }

        return availableTimes;
    }

    function populateAvailableTimeSlots(availableTimes, numberOfGuests, date, reservations) {
        timeSlotsSelect.innerHTML = '';

        if (availableTimes && availableTimes.length > 0) {
            timeSlotsSelect.innerHTML = '<option value="">Select a time slot</option>';
            availableTimes.forEach(function (time) {
                const option = document.createElement('option');
                option.value = time.value;
                option.textContent = time.display;
                timeSlotsSelect.appendChild(option);
            });
            timeSlotsSelect.disabled = false;
        } else {
            timeSlotsSelect.innerHTML = '<option value="">No available time slots for the selected date</option>';
            timeSlotsSelect.disabled = true;
        }

        timeSlotsSelect.addEventListener('change', function () {
            const selectedTime = this.value;
            if (selectedTime) {
                const availableTables = getAvailableTables(date, selectedTime, numberOfGuests, reservations);
                populateAvailableTables(availableTables, numberOfGuests);
                console.log('Selected time:', selectedTime)
                updateReservationDateTime(date, selectedTime);
            }
        });
    }

    function getAvailableTables(date, selectedTime, numberOfGuests, reservations) {
        const [selectedHours, selectedMinutes] = selectedTime.split(':').map(Number);
        const selectedDateTime = new Date(date);
        selectedDateTime.setHours(selectedHours, selectedMinutes);

        const occupiedTableIds = reservations
            .filter(reservation => {
                const reservationDate = new Date(reservation.reservationDate);
                const timeDiff = Math.abs(reservationDate - selectedDateTime);
                return timeDiff < 2 * 60 * 60 * 1000; // Within 2 hours
            })
            .flatMap(reservation => reservation.tables.map(table => table.tableId));

        return allTables.filter(table =>
            !occupiedTableIds.includes(table.tableId) && table.seatingCapacity >= numberOfGuests
        );
    }

    function populateAvailableTables(tables, numberOfGuests) {
        tablesDiv.innerHTML = '';

        if (tables && tables.length > 0) {
            tables.forEach(function (table) {
                const checkbox = document.createElement('input');
                checkbox.type = 'checkbox';
                checkbox.name = 'TableNumbers';
                checkbox.value = table.tableNumber;
                checkbox.id = 'table-' + table.tableId;

                const label = document.createElement('label');
                label.htmlFor = 'table-' + table.tableId;
                label.textContent = `Table ${table.tableNumber} (${table.seatingCapacity} seats, ${table.location})`;

                tablesDiv.appendChild(checkbox);
                tablesDiv.appendChild(label);
                tablesDiv.appendChild(document.createElement('br'));
            });

            tablesDiv.querySelectorAll('input[type="checkbox"]').forEach(checkbox => {
                checkbox.addEventListener('change', () => checkIfEnoughTablesSelected(tables, numberOfGuests));
            });

            tablesDiv.disabled = false;
        } else {
            tablesDiv.textContent = 'No available tables for the selected time and number of guests.';
            reserveButton.disabled = true;
        }
    }

    function checkIfEnoughTablesSelected(tables, numberOfGuests) {
        const selectedTables = Array.from(tablesDiv.querySelectorAll('input[type="checkbox"]:checked'))
            .map(checkbox => tables.find(table => table.tableNumber === parseInt(checkbox.value)));

        const totalSeats = selectedTables.reduce((sum, table) => sum + table.seatingCapacity, 0);

        reserveButton.disabled = totalSeats < numberOfGuests;
    }

    function updateReservationDateTime(date, time) {
        const [hours, minutes] = time.split(':');
        const reservationDateTime = new Date(date);
        reservationDateTime.setHours(hours, minutes, 0, 0);
        document.getElementById('hiddenReservationDate').value = reservationDateTime.toLocaleString();
        console.log('Formatted date: ', document.getElementById('hiddenReservationDate').value);
    }

        

    form.addEventListener('submit', function (e) {
        e.preventDefault();

        if (reserveButton.disabled) {
            alert('Please ensure all fields are filled correctly and sufficient tables are selected.');
            return;
        }

        // Log form data for debugging, exactly as it is
        const formData = new FormData(form);
        for (let [key, value] of formData.entries()) {
            console.log(`${key}: ${value}`);
        }


        // Form is valid, submit it
        this.submit();
    });
}