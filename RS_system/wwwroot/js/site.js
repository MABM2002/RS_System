// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

$(document).ready(function () {
    const sidebar = $('.sidebar');
    const sidebarOverlay = $('#sidebarOverlay');
    const sidebarToggle = $('#sidebarToggle');
    const mainContent = $('.main-content');

    // Toggle Sidebar
    sidebarToggle.on('click', function (e) {
        e.stopPropagation();
        sidebar.toggleClass('open');
        sidebarOverlay.toggleClass('show');
        
        // Desktop collapse behavior
        if (window.innerWidth > 768) {
            sidebar.toggleClass('collapsed');
            mainContent.toggleClass('expanded');
        }
    });

    // Close sidebar when clicking overlay (mobile)
    sidebarOverlay.on('click', function () {
        sidebar.removeClass('open');
        sidebarOverlay.removeClass('show');
    });

    // Close sidebar when clicking outside (mobile) - optional extra safety
    $(document).on('click', function (e) {
        if (window.innerWidth <= 768) {
            if (!sidebar.is(e.target) && sidebar.has(e.target).length === 0 && 
                !sidebarToggle.is(e.target) && sidebarToggle.has(e.target).length === 0) {
                sidebar.removeClass('open');
                sidebarOverlay.removeClass('show');
            }
        }
    });
});
