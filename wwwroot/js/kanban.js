(function () {
    'use strict';

    function antiforgeryToken() {
        var input = document.querySelector('#antiforgery input[name="__RequestVerificationToken"]');
        return input ? input.value : '';
    }

    function authHeaders() {
        return { 'RequestVerificationToken': antiforgeryToken() };
    }

    async function post(url) {
        return fetch(url, { method: 'POST', headers: authHeaders() });
    }

    // ---------- Drag & drop ----------
    var dragged = null;

    document.addEventListener('dragstart', function (e) {
        var card = e.target.closest('.kanban-card');
        if (!card) return;
        dragged = card;
        card.classList.add('dragging');
        e.dataTransfer.effectAllowed = 'move';
    });

    document.addEventListener('dragend', function () {
        if (dragged) dragged.classList.remove('dragging');
        dragged = null;
        document.querySelectorAll('.drag-over').forEach(function (el) {
            el.classList.remove('drag-over');
        });
    });

    document.addEventListener('dragover', function (e) {
        var body = e.target.closest('.kanban-column-body');
        if (!body || !dragged) return;
        e.preventDefault();
        body.classList.add('drag-over');
    });

    document.addEventListener('dragleave', function (e) {
        var body = e.target.closest('.kanban-column-body');
        if (body) body.classList.remove('drag-over');
    });

    function refreshCount(column) {
        var pill = column.querySelector('.count-pill');
        if (pill) pill.textContent = column.querySelectorAll('.kanban-card').length;
    }

    document.addEventListener('drop', async function (e) {
        var body = e.target.closest('.kanban-column-body');
        if (!body || !dragged) return;
        e.preventDefault();
        body.classList.remove('drag-over');

        // dragend dispara al soltar y limpia la variable global antes del fetch.
        var card = dragged;
        var column = body.closest('.kanban-column');
        var taskId = parseInt(card.dataset.taskId, 10);
        var statusId = parseInt(column.dataset.statusId, 10);

        var response = await fetch('/Tasks/ChangeStatus', {
            method: 'POST',
            headers: Object.assign({ 'Content-Type': 'application/json' }, authHeaders()),
            body: JSON.stringify({ taskId: taskId, statusId: statusId })
        });

        if (response.ok) {
            var source = card.closest('.kanban-column');
            body.appendChild(card);
            card.dataset.statusId = statusId;
            refreshCount(source);
            refreshCount(column);
        } else {
            alert('No se pudo mover la tarea.');
            location.reload();
        }
    });

    // ---------- Modales ----------
    function showModal(modalEl) {
        var container = document.getElementById('modal-container');
        // Quita el foco antes de que Bootstrap oculte el modal con aria-hidden.
        modalEl.addEventListener('hide.bs.modal', function () {
            if (modalEl.contains(document.activeElement)) document.activeElement.blur();
        });
        modalEl.addEventListener('hidden.bs.modal', function () {
            container.innerHTML = '';
        });
        new bootstrap.Modal(modalEl).show();
    }

    async function openModal(url) {
        var container = document.getElementById('modal-container');
        var open = document.querySelector('#modal-container .modal');
        if (open) {
            var instance = bootstrap.Modal.getInstance(open);
            if (instance) instance.hide();
            await new Promise(function (resolve) { setTimeout(resolve, 200); });
        }

        var response = await fetch(url);
        if (!response.ok) {
            alert('No se pudo abrir la ventana.');
            return;
        }

        container.innerHTML = await response.text();
        var modalEl = container.querySelector('.modal');
        if (modalEl) showModal(modalEl);
    }

    // ---------- Acciones ----------
    document.addEventListener('click', function (e) {
        var opener = e.target.closest('[data-modal-url]');
        if (opener) {
            e.preventDefault();
            openModal(opener.dataset.modalUrl);
            return;
        }

        var deleteTask = e.target.closest('[data-delete-task]');
        if (deleteTask) {
            e.preventDefault();
            if (!confirm('¿Eliminar esta tarea?')) return;
            post('/Tasks/Delete/' + deleteTask.dataset.deleteTask).then(function (response) {
                if (response.ok) location.reload();
                else alert('No se pudo eliminar la tarea.');
            });
            return;
        }

        var deleteChecklist = e.target.closest('[data-delete-checklist]');
        if (deleteChecklist) {
            e.preventDefault();
            var taskId = deleteChecklist.dataset.taskId;
            var itemId = deleteChecklist.dataset.deleteChecklist;
            post('/Tasks/DeleteChecklistItem?checklistItemId=' + itemId + '&taskId=' + taskId)
                .then(replaceChecklist);
        }
    });

    document.addEventListener('change', function (e) {
        var toggle = e.target.closest('[data-toggle-checklist]');
        if (!toggle) return;

        var taskId = toggle.dataset.taskId;
        var itemId = toggle.dataset.toggleChecklist;
        post('/Tasks/ToggleChecklistItem?checklistItemId=' + itemId + '&taskId=' + taskId)
            .then(replaceChecklist);
    });

    async function replaceChecklist(response) {
        if (!response.ok) {
            alert('No se pudo actualizar la subtarea.');
            return;
        }
        var container = document.getElementById('checklist-container');
        if (container) container.innerHTML = await response.text();
    }

    // ---------- Formularios AJAX ----------
    document.addEventListener('submit', async function (e) {
        var form = e.target;

        if (form.matches('[data-ajax-form]')) {
            e.preventDefault();
            var response = await fetch(form.action, {
                method: 'POST',
                body: new FormData(form),
                headers: authHeaders()
            });

            var contentType = response.headers.get('content-type') || '';
            if (contentType.indexOf('application/json') >= 0) {
                var data = await response.json();
                if (data.success) {
                    location.reload();
                } else {
                    alert(data.message || 'No se pudo guardar.');
                }
            } else {
                // Errores de validación: se vuelve a mostrar el modal con los mensajes.
                var container = document.getElementById('modal-container');
                var open = container.querySelector('.modal');
                if (open) {
                    var instance = bootstrap.Modal.getInstance(open);
                    if (instance) instance.hide();
                    await new Promise(function (resolve) { setTimeout(resolve, 200); });
                }
                container.innerHTML = await response.text();
                var modalEl = container.querySelector('.modal');
                if (modalEl) showModal(modalEl);
            }
            return;
        }

        if (form.matches('[data-comment-form]') || form.matches('[data-checklist-form]')) {
            e.preventDefault();
            var res = await fetch(form.action, {
                method: 'POST',
                body: new FormData(form),
                headers: authHeaders()
            });
            if (!res.ok) {
                alert('No se pudo guardar.');
                return;
            }
            var html = await res.text();
            var target = form.matches('[data-comment-form]')
                ? document.getElementById('comments-container')
                : document.getElementById('checklist-container');
            if (target) target.innerHTML = html;
            form.reset();
        }
    });

    // Confirmación declarativa: data-confirm="mensaje" (evita onsubmit inline, bloqueado por CSP).
    document.addEventListener('submit', function (e) {
        var message = e.target.dataset ? e.target.dataset.confirm : null;
        if (message && !confirm(message)) e.preventDefault();
    });
})();
