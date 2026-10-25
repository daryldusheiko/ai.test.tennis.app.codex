window.tennisSchedule = {
    attach(element, receiver) {
        let drag;
        const clear = () => element.querySelectorAll('.dragging, .drop-target').forEach(item => item.classList.remove('dragging', 'drop-target'));
        const down = event => {
            const handle = event.target.closest('[data-handle]');
            if (!handle || event.button !== 0) return;
            event.preventDefault();
            const round = handle.closest('[data-round]');
            drag = { pointer: event.pointerId, round: Number(round.dataset.round),
                game: handle.dataset.handle === 'game' ? Number(handle.closest('[data-game]').dataset.game) : -1 };
            (drag.game < 0 ? round : handle.closest('[data-game]')).classList.add('dragging');
            handle.setPointerCapture(event.pointerId);
        };
        const move = event => {
            if (!drag) return;
            event.preventDefault();
            element.querySelectorAll('.drop-target').forEach(item => item.classList.remove('drop-target'));
            const hover = document.elementFromPoint(event.clientX, event.clientY);
            const target = hover?.closest(drag.game < 0 ? '[data-round]' : '[data-game]');
            if (target && element.contains(target)) target.classList.add('drop-target');
            // Scrolling at the viewport edges lets touch users move cards beyond the initial screen.
            if (event.clientY < 90) window.scrollBy(0, -18);
            if (event.clientY > innerHeight - 110) window.scrollBy(0, 18);
        };
        const up = event => {
            if (!drag || drag.pointer !== event.pointerId) return;
            const source = drag;
            drag = null;
            clear();
            const target = document.elementFromPoint(event.clientX, event.clientY);
            const round = target?.closest('[data-round]');
            if (!round || !element.contains(round)) return;
            if (source.game < 0) receiver.invokeMethodAsync('MoveAsync', source.round, -1, Number(round.dataset.round));
            else {
                const game = target.closest('[data-game]');
                if (game && Number(round.dataset.round) === source.round)
                    receiver.invokeMethodAsync('MoveAsync', source.round, source.game, Number(game.dataset.game));
            }
        };
        const cancel = () => { drag = null; clear(); };
        element.addEventListener('pointerdown', down);
        element.addEventListener('pointermove', move);
        element.addEventListener('pointerup', up);
        element.addEventListener('pointercancel', cancel);
        element._scheduleDetach = () => {
            element.removeEventListener('pointerdown', down);
            element.removeEventListener('pointermove', move);
            element.removeEventListener('pointerup', up);
            element.removeEventListener('pointercancel', cancel);
        };
    },
    detach(element) { element._scheduleDetach?.(); }
};
