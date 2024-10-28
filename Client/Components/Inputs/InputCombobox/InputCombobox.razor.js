/**
 * Creates and initializes an `InputCombobox` instance.
 *
 * @param {object} dotNetObject - The .NET object that will handle updates when an option is selected.
 * @param {string} uid - The unique identifier for the input element.
 * @param {Array<string>} options - The list of options to populate the combobox.
 * @returns {InputCombobox} - The created `InputCombobox` instance.
 */
export function createInputCombobox(dotNetObject, uid, options) {
    return new InputCombobox(dotNetObject, uid, options);
}

/**
 * Class representing a combobox with dynamic filtering and selection functionality.
 */
export class InputCombobox {
    /**
     * Creates an instance of the `InputCombobox` class.
     *
     * @param {object} dotNetObject - The .NET object that will handle updates when an option is selected.
     * @param {string} uid - The unique identifier for the input element.
     * @param {Array<string>} options - The list of options to populate the combobox.
     */
    constructor(dotNetObject, uid, options) {
        this.dotNetObject = dotNetObject;
        this.uid = uid;
        this.options = options;
        this.filteredOptions = [...options]; // Initially, filtered options are all options
        this.dropdownVisible = false;
        this.selectedIndex = -1; // No selection initially

        // Reference to the combobox element
        this.combobox = document.getElementById(uid);

        // Create dropdown element
        this.dropdown = document.createElement("ul");
        this.dropdown.className = "combobox-dropdown";
        this.dropdown.style.display = "none"; // Hidden by default

        // Append the dropdown within the same parent as the input
        this.combobox.parentElement.appendChild(this.dropdown);

        // Populate dropdown with options
        this.renderDropdown();

        // Event listeners
        this.combobox.addEventListener("input", this.handleInput.bind(this));
        this.combobox.addEventListener("keydown", this.handleKeyDown.bind(this));
        this.combobox.addEventListener("blur", this.handleBlur.bind(this));
        this.combobox.addEventListener("focus", this.showDropdown.bind(this));
    }

    /**
     * Renders the dropdown options based on the filtered options.
     * Hides the dropdown if there are no options.
     */
    renderDropdown() {
        this.dropdown.innerHTML = "";
        if (this.filteredOptions.length === 0) {
            this.hideDropdown(); // If no options, hide the dropdown
            return;
        }

        this.filteredOptions.forEach((option, index) => {
            const li = document.createElement("li");
            li.className = "combobox-option";
            li.textContent = option;
            li.setAttribute("data-index", index);
            li.addEventListener("mousedown", () => {
                this.selectOption(option);
            });

            if (this.selectedIndex === index) {
                li.classList.add("selected");
            }

            this.dropdown.appendChild(li);
        });
    }

    /**
     * Handles input events on the combobox.
     * Filters the options based on the input value and updates the dropdown.
     *
     * @param {Event} event - The input event.
     */
    handleInput(event) {
        const inputValue = event.target.value.toLowerCase();

        // Filter options
        this.filteredOptions = this.options.filter(option =>
            option.toLowerCase().includes(inputValue)
        );

        // Reset selected index when typing
        this.selectedIndex = -1;

        this.renderDropdown();
        if (!this.dropdownVisible) {
            this.showDropdown();
        }
    }

    /**
     * Handles keydown events on the combobox.
     * Allows navigation through the options with arrow keys and selection with Enter key.
     *
     * @param {Event} event - The keydown event.
     */
    handleKeyDown(event) {
        if (this.dropdownVisible) {
            if (event.key === "ArrowDown") {
                this.selectedIndex = Math.min(this.selectedIndex + 1, this.filteredOptions.length - 1);
                this.renderDropdown();
                event.preventDefault(); // Prevent cursor moving in the input
            } else if (event.key === "ArrowUp") {
                this.selectedIndex = Math.max(this.selectedIndex - 1, 0);
                this.renderDropdown();
                event.preventDefault();
            } else if (event.key === "Enter") {
                if (this.selectedIndex >= 0 && this.selectedIndex < this.filteredOptions.length) {
                    this.selectOption(this.filteredOptions[this.selectedIndex]);
                    event.preventDefault(); // Prevent form submission, if inside a form
                }
            }
        }
    }

    /**
     * Handles blur events on the combobox.
     * Hides the dropdown after a short delay to allow option click.
     */
    handleBlur() {
        // Hide the dropdown after a short delay to allow option click
        setTimeout(() => {
            this.hideDropdown();
        }, 200);
    }

    /**
     * Shows the dropdown.
     * Positions it relative to the combobox element.
     */
    showDropdown() {
        if (this.filteredOptions.length > 0) { // Only show dropdown if options are available
            this.dropdown.style.display = "block";
            this.dropdownVisible = true;
        }
    }

    /**
     * Hides the dropdown.
     */
    hideDropdown() {
        this.dropdown.style.display = "none";
        this.dropdownVisible = false;
    }

    /**
     * Selects an option from the dropdown and updates the combobox value.
     *
     * @param {string} option - The selected option.
     */
    selectOption(option) {
        this.combobox.value = option;
        this.dotNetObject.invokeMethodAsync("UpdateValue", option);
        this.hideDropdown();
    }
}
