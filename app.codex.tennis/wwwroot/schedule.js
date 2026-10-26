window.tennisSchedule = {
    attach(element, receiver) {
        let drag;
        let hold;
        let suppressClick = false;
        const cancelHold = () => { if (hold) clearTimeout(hold.timer); hold = null; };
        const click = event => {
            if (suppressClick) { event.preventDefault(); event.stopImmediatePropagation(); suppressClick = false; }
        };
        const clear = () => element.querySelectorAll('.dragging, .drop-target').forEach(item => item.classList.remove('dragging', 'drop-target'));
        const down = event => {
            suppressClick = false;
            const player = event.target.closest('[data-player]');
            if (player && element.dataset.editing === 'true' && event.button === 0) {
                const round = Number(player.closest('[data-round]').dataset.round);
                hold = { x: event.clientX, y: event.clientY, timer: setTimeout(() => {
                    hold = null;
                    suppressClick = true;
                    receiver.invokeMethodAsync('HoldAsync', round, player.dataset.player);
                }, 500) };
                return;
            }
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
            if (hold && Math.hypot(event.clientX - hold.x, event.clientY - hold.y) > 10) cancelHold();
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
            cancelHold();
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
        const cancel = () => { cancelHold(); drag = null; clear(); };
        element.addEventListener('click', click, true);
        element.addEventListener('pointerdown', down);
        element.addEventListener('pointermove', move);
        element.addEventListener('pointerup', up);
        element.addEventListener('pointercancel', cancel);
        element._scheduleDetach = () => {
            cancelHold();
            element.removeEventListener('click', click, true);
            element.removeEventListener('pointerdown', down);
            element.removeEventListener('pointermove', move);
            element.removeEventListener('pointerup', up);
            element.removeEventListener('pointercancel', cancel);
        };
    },
    detach(element) { element._scheduleDetach?.(); }
};
