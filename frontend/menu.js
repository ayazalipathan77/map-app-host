document.addEventListener('DOMContentLoaded', () => {
    const menuIcon = document.getElementById('menuIcon');
    const sideMenu = document.getElementById('sideMenu');
    const closeBtn = sideMenu.querySelector('.close-btn');
    const overlay = document.getElementById('overlay');

    const loginLink = document.getElementById('loginLink');
    const logoutLink = document.getElementById('logoutLink');

    // Ensure image dialog is hidden on page load
    const imageDialog = document.getElementById('imageDialog');
    if (imageDialog) {
        imageDialog.style.display = 'none';
    }

    function openNav() {
        sideMenu.style.width = '250px'; // Adjust width as needed
        overlay.style.width = '100%';
        document.body.classList.add('menu-open'); // Add class to body to prevent scrolling
    }

    function closeNav() {
        sideMenu.style.width = '0';
        overlay.style.width = '0';
        document.body.classList.remove('menu-open');
    }

    // Function to update login/logout links visibility
    function updateMenuLinks() {
        const jwtToken = localStorage.getItem('jwtToken');
        const isAdminLoggedIn = !!jwtToken;
        if (loginLink && logoutLink) {
            if (isAdminLoggedIn) {
                loginLink.style.display = 'none';
                logoutLink.style.display = 'block';
            } else {
                loginLink.style.display = 'block';
                logoutLink.style.display = 'none';
            }
        }
    }

    // Logout functionality
    if (logoutLink) {
        logoutLink.addEventListener('click', (e) => {
            e.preventDefault(); // Prevent default link behavior
            localStorage.removeItem('jwtToken');
            window.location.href = 'login.html';
        });
    }

    if (menuIcon) {
        menuIcon.addEventListener('click', openNav);
    }
    if (closeBtn) {
        closeBtn.addEventListener('click', closeNav);
    }
    if (overlay) {
        overlay.addEventListener('click', closeNav);
    }

    // Initial update of menu links
    updateMenuLinks();
});