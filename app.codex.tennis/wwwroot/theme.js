(() => {
    const device = matchMedia('(prefers-color-scheme: dark)');
    let choice = 'Device';
    try { choice = localStorage.getItem('social-tennis-theme') || 'Device'; } catch { /* Device theme still works if storage is unavailable. */ }
    const apply = () => document.documentElement.dataset.theme = choice === 'Device' ? (device.matches ? 'Dark' : 'Light') : choice;
    window.tennisTheme = {
        get: () => choice,
        set: value => {
            choice = ['Light', 'Dark', 'Device'].includes(value) ? value : 'Device';
            try { localStorage.setItem('social-tennis-theme', choice); } catch { /* Keep the current session usable. */ }
            apply();
        }
    };
    if (device.addEventListener) device.addEventListener('change', apply);
    else device.addListener(apply);
    apply();
})();
