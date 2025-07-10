document.addEventListener('DOMContentLoaded', () => {
    const menuIcon = document.getElementById('menuIcon');
    const sideMenu = document.getElementById('sideMenu');

    const loginLink = document.getElementById('loginLink');
    const logoutLink = document.getElementById('logoutLink');
    const loginLinkMobile = document.getElementById('loginLinkMobile');
    const logoutLinkMobile = document.getElementById('logoutLinkMobile');

    // Ensure image dialog is hidden on page load
    const imageDialog = document.getElementById('imageDialog');
    if (imageDialog) {
        imageDialog.style.display = 'none';
    }

    function toggleMenu() {
        sideMenu.classList.toggle('hidden');
    }

    // Function to update login/logout links visibility
    function updateMenuLinks() {
        const jwtToken = localStorage.getItem('jwtToken');
        const isAdminLoggedIn = !!jwtToken;

        if (isAdminLoggedIn) {
            if (loginLink) loginLink.style.display = 'none';
            if (logoutLink) logoutLink.style.display = 'block';
            if (loginLinkMobile) loginLinkMobile.style.display = 'none';
            if (logoutLinkMobile) logoutLinkMobile.style.display = 'block';
        } else {
            if (loginLink) loginLink.style.display = 'block';
            if (logoutLink) logoutLink.style.display = 'none';
            if (loginLinkMobile) loginLinkMobile.style.display = 'block';
            if (logoutLinkMobile) logoutLinkMobile.style.display = 'none';
        }
    }

    // Logout functionality
    if (logoutLink) {
        logoutLink.addEventListener('click', (e) => {
            e.preventDefault();
            localStorage.removeItem('jwtToken');
            window.location.href = 'login.html';
        });
    }
    if (logoutLinkMobile) {
        logoutLinkMobile.addEventListener('click', (e) => {
            e.preventDefault();
            localStorage.removeItem('jwtToken');
            window.location.href = 'login.html';
        });
    }

    if (menuIcon) {
        menuIcon.addEventListener('click', toggleMenu);
    }

    // Initial update of menu links
    updateMenuLinks();
});