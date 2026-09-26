/* Personal Kanban JavaScript - Drag & Drop & Interactive Modals */

document.addEventListener('DOMContentLoaded', () => {
    initTheme();
    initDragAndDrop();
});

// Theme Management (Light / Dark mode)
function initTheme() {
    const savedTheme = localStorage.getItem('kanban_theme') || 'light';
    setTheme(savedTheme);

    const themeToggleBtn = document.getElementById('themeToggleBtn');
    if (themeToggleBtn) {
        themeToggleBtn.addEventListener('click', () => {
            const currentTheme = document.documentElement.getAttribute('data-bs-theme') || 'light';
            const newTheme = currentTheme === 'light' ? 'dark' : 'light';
            setTheme(newTheme);
        });
    }
}

function setTheme(theme) {
    document.documentElement.setAttribute('data-bs-theme', theme);
    localStorage.setItem('kanban_theme', theme);
    
    const icon = document.getElementById('themeIcon');
    if (icon) {
        icon.className = theme === 'dark' ? 'bi bi-sun-fill text-warning' : 'bi bi-moon-stars-fill text-primary';
    }
}

// Drag & Drop Handler
function initDragAndDrop() {
    const cards = document.querySelectorAll('.kanban-card');
    const containers = document.querySelectorAll('.kanban-cards-wrapper');

    cards.forEach(card => {
        card.addEventListener('dragstart', handleDragStart);
        card.addEventListener('dragend', handleDragEnd);
    });

    containers.forEach(container => {
        container.addEventListener('dragover', handleDragOver);
        container.addEventListener('dragenter', handleDragEnter);
        container.addEventListener('dragleave', handleDragLeave);
        container.addEventListener('drop', handleDrop);
    });
}

let draggedCard = null;

function handleDragStart(e) {
    draggedCard = this;
    this.classList.add('dragging');
    e.dataTransfer.effectAllowed = 'move';
    e.dataTransfer.setData('text/plain', this.dataset.taskId);
}

function handleDragEnd(e) {
    this.classList.remove('dragging');
    draggedCard = null;
    document.querySelectorAll('.kanban-cards-wrapper').forEach(c => c.classList.remove('drag-over'));
}

function handleDragOver(e) {
    if (e.preventDefault) {
        e.preventDefault();
    }
    e.dataTransfer.dropEffect = 'move';
    return false;
}

function handleDragEnter(e) {
    this.classList.add('drag-over');
}

function handleDragLeave(e) {
    if (e.target === this) {
        this.classList.remove('drag-over');
    }
}

async function handleDrop(e) {
    e.preventDefault();
    this.classList.remove('drag-over');

    if (!draggedCard) return;

    const taskId = parseInt(draggedCard.dataset.taskId);
    const newStatusId = parseInt(this.dataset.statusId);
    const currentStatusId = parseInt(draggedCard.dataset.statusId);

    if (newStatusId === currentStatusId) return;

    // Visual update first
    this.appendChild(draggedCard);
    draggedCard.dataset.statusId = newStatusId;

    updateColumnCounts();

    // Send AJAX request
    try {
        const response = await fetch('/Tasks/ChangeStatus', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'X-Requested-With': 'XMLHttpRequest'
            },
            body: JSON.stringify({
                taskId: taskId,
                statusId: newStatusId
            })
        });

        const data = await response.json();
        if (!response.ok || !data.success) {
            showToast('Error', data.message || 'No se pudo cambiar el estado de la tarea.', 'danger');
            // Revert movement if failed
            location.reload();
        } else {
            showToast('Estado Actualizado', 'La tarea se movió correctamente.', 'success');
        }
    } catch (err) {
        console.error('Error changing status:', err);
        showToast('Error de conexión', 'Ocurrió un fallo al comunicar con el servidor.', 'danger');
        location.reload();
    }
}

function updateColumnCounts() {
    document.querySelectorAll('.kanban-column').forEach(col => {
        const countBadge = col.querySelector('.kanban-column-count');
        const cards = col.querySelectorAll('.kanban-card');
        if (countBadge) {
            countBadge.textContent = cards.length;
        }
    });
}

// Modal Loaders
async function openCreateTaskModal() {
    try {
        const res = await fetch('/Tasks/Create');
        const html = await res.text();
        document.getElementById('modalContainer').innerHTML = html;
        const modal = new bootstrap.Modal(document.getElementById('taskFormModal'));
        modal.show();
    } catch (err) {
        console.error(err);
    }
}

async function openEditTaskModal(taskId) {
    try {
        const res = await fetch(`/Tasks/Edit/${taskId}`);
        const html = await res.text();
        document.getElementById('modalContainer').innerHTML = html;
        const modal = new bootstrap.Modal(document.getElementById('taskFormModal'));
        modal.show();
    } catch (err) {
        console.error(err);
    }
}

async function openTaskDetailsModal(taskId) {
    try {
        const res = await fetch(`/Tasks/Details/${taskId}`);
        const html = await res.text();
        document.getElementById('modalContainer').innerHTML = html;
        const modal = new bootstrap.Modal(document.getElementById('taskDetailModal'));
        modal.show();
    } catch (err) {
        console.error(err);
    }
}

async function submitTaskForm(formElement) {
    const formData = new FormData(formElement);
    try {
        const res = await fetch(formElement.action, {
            method: 'POST',
            body: formData
        });

        if (res.headers.get('content-type')?.includes('application/json')) {
            const data = await res.json();
            if (data.success) {
                const modalEl = document.getElementById('taskFormModal');
                const modal = bootstrap.Modal.getInstance(modalEl);
                if (modal) modal.hide();
                location.reload();
            } else {
                showToast('Error', data.message, 'danger');
            }
        } else {
            // Re-render form validation errors inside modal
            const html = await res.text();
            document.getElementById('modalContainer').innerHTML = html;
            const modal = new bootstrap.Modal(document.getElementById('taskFormModal'));
            modal.show();
        }
    } catch (err) {
        console.error(err);
    }
}

async function confirmDeleteTask(taskId) {
    if (confirm('¿Está seguro de eliminar esta tarea? Se guardará en la papelera para evitar pérdidas accidentales.')) {
        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
        const formData = new FormData();
        if (token) formData.append('__RequestVerificationToken', token);
        formData.append('id', taskId);

        try {
            const res = await fetch('/Tasks/Delete', {
                method: 'POST',
                body: formData
            });

            const data = await res.json();
            if (data.success) {
                showToast('Eliminada', data.message, 'success');
                location.reload();
            } else {
                showToast('Error', data.message, 'danger');
            }
        } catch (err) {
            console.error(err);
        }
    }
}

// Checklist Helpers in Modal
function addChecklistInput() {
    const container = document.getElementById('checklistInputsContainer');
    if (!container) return;
    const div = document.createElement('div');
    div.className = 'input-group mb-2';
    div.innerHTML = `
        <input type="text" name="NewChecklistItems" class="form-control form-control-sm" placeholder="Nueva subtarea..." />
        <button type="button" class="btn btn-outline-danger btn-sm" onclick="this.parentElement.remove()">
            <i class="bi bi-trash"></i>
        </button>
    `;
    container.appendChild(div);
}

// Toast Notifications
function showToast(title, message, type = 'info') {
    const toastContainer = document.getElementById('toastContainer');
    if (!toastContainer) return;

    const bgClass = type === 'success' ? 'bg-success text-white' : type === 'danger' ? 'bg-danger text-white' : 'bg-primary text-white';
    
    const toastEl = document.createElement('div');
    toastEl.className = `toast align-items-center ${bgClass} border-0 show mb-2`;
    toastEl.setAttribute('role', 'alert');
    toastEl.innerHTML = `
        <div class="d-flex">
            <div class="toast-body">
                <strong>${title}:</strong> ${message}
            </div>
            <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button>
        </div>
    `;

    toastContainer.appendChild(toastEl);
    setTimeout(() => {
        toastEl.remove();
    }, 4000);
}
