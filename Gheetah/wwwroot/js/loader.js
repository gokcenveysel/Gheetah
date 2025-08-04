(function() {
    const loaderElement = document.getElementById('globalLoader');
    const messageElement = document.getElementById('loaderMessage');
    
    if (!loaderElement || !messageElement) {
        console.error('Loader elements not found! Will create fallback');
    }

    window.Loader = {
        show: function(message = "Processing your request...", options = {}) {
            console.log("[Loader] Showing with message:", message);
            
            if (!loaderElement) {
                console.error("[Loader] Loader element not found!");
                return false;
            }

            messageElement.textContent = message;
            loaderElement.classList.add('active');
            
            if (options.color) {
                const icon = loaderElement.querySelector('.loader-icon');
                if (icon) icon.style.stroke = options.color;
            }
            
            return true;
        },

        hide: function() {
            console.log("[Loader] Hiding");
            if (loaderElement) {
                loaderElement.classList.remove('active');
                return true;
            }
            return false;
        },

        setMessage: function(message) {
            console.log("[Loader] Setting message:", message);
            if (messageElement) {
                messageElement.textContent = message;
                return true;
            }
            return false;
        }
    };
})();