const API_BASE = '';
let token = null;
let currentUser = null;

// Utility functions
function showMessage(text, type = 'success') {
    const msg = document.getElementById('message');
    msg.textContent = text;
    msg.className = type;
    msg.classList.remove('hidden');
    setTimeout(() => msg.classList.add('hidden'), 3000);
}

async function apiCall(endpoint, method = 'GET', body = null) {
    const headers = { 'Content-Type': 'application/json' };
    if (token) headers['Authorization'] = `Bearer ${token}`;
    
    const options = { method, headers };
    if (body) options.body = JSON.stringify(body);
    
    const response = await fetch(API_BASE + endpoint, options);
    
    if (response.status === 401) {
        logout();
        throw new Error('Session expired. Please login again.');
    }
    
    if (response.status === 403) {
        throw new Error('Access denied. You do not have permission or consent is not granted.');
    }
    
    if (!response.ok) {
        const err = await response.json().catch(() => ({}));
        throw new Error(err.errors?.join(', ') || err.title || 'Request failed');
    }
    
    if (response.status === 204) return null;
    return response.json();
}

// Auth functions
document.getElementById('login-form').addEventListener('submit', async (e) => {
    e.preventDefault();
    const email = document.getElementById('email').value;
    const password = document.getElementById('password').value;
    
    try {
        const data = await apiCall('/api/auth/login', 'POST', { email, password });
        token = data.accessToken;
        currentUser = data;
        localStorage.setItem('token', token);
        localStorage.setItem('user', JSON.stringify(data));
        showDashboard();
        showMessage('Login successful!');
    } catch (err) {
        document.getElementById('login-error').textContent = 'Invalid credentials';
        document.getElementById('login-error').classList.remove('hidden');
    }
});

// OTP Login for Patients
let pendingPatientId = null;

document.getElementById('request-otp-form').addEventListener('submit', async (e) => {
    e.preventDefault();
    const patientIdOrMrn = document.getElementById('patient-id-login').value;
    
    try {
        const data = await apiCall('/api/auth/request-otp', 'POST', { patientIdOrMrn });
        pendingPatientId = data.patientId; // Use the resolved GUID from server
        document.getElementById('otp-display').innerHTML = `
            <strong>Welcome, ${data.patientName}!</strong><br>
            Your OTP: <strong style="font-size: 1.5em; letter-spacing: 3px;">${data.otp}</strong><br>
            <small>(Valid for 10 minutes - In production, this would be sent via SMS/Email)</small>
        `;
        document.getElementById('otp-request-section').classList.add('hidden');
        document.getElementById('otp-verify-section').classList.remove('hidden');
        document.getElementById('otp-code').focus();
    } catch (err) {
        showMessage(err.message || 'Patient not found. Use MRN (e.g., P001) or Patient ID', 'error');
    }
});

document.getElementById('otp-login-form').addEventListener('submit', async (e) => {
    e.preventDefault();
    const otp = document.getElementById('otp-code').value;
    
    try {
        const data = await apiCall('/api/auth/login-otp', 'POST', { patientId: pendingPatientId, otp });
        token = data.accessToken;
        currentUser = data;
        localStorage.setItem('token', token);
        localStorage.setItem('user', JSON.stringify(data));
        showDashboard();
        showMessage('Login successful!');
        resetOtpForm();
    } catch (err) {
        showMessage(err.message || 'Invalid OTP', 'error');
    }
});

document.getElementById('back-to-request').addEventListener('click', resetOtpForm);

function resetOtpForm() {
    pendingPatientId = null;
    document.getElementById('otp-request-section').classList.remove('hidden');
    document.getElementById('otp-verify-section').classList.add('hidden');
    document.getElementById('patient-id-login').value = '';
    document.getElementById('otp-code').value = '';
}

function logout() {
    token = null;
    currentUser = null;
    localStorage.removeItem('token');
    localStorage.removeItem('user');
    document.getElementById('login-section').classList.remove('hidden');
    document.getElementById('dashboard').classList.add('hidden');
    document.getElementById('email').value = '';
    document.getElementById('password').value = '';
}

document.getElementById('logout-btn').addEventListener('click', logout);

function showDashboard() {
    document.getElementById('login-section').classList.add('hidden');
    document.getElementById('login-error').classList.add('hidden');
    document.getElementById('dashboard').classList.remove('hidden');
    
    // Ensure roles is always an array
    const roles = Array.isArray(currentUser.roles) ? currentUser.roles : [currentUser.roles].filter(Boolean);
    currentUser.roles = roles;
    
    document.getElementById('user-email').textContent = currentUser.email;
    document.getElementById('user-role').textContent = roles.join(', ');
    
    // Hide all role sections
    document.getElementById('admin-section').classList.add('hidden');
    document.getElementById('doctor-section').classList.add('hidden');
    document.getElementById('nurse-section').classList.add('hidden');
    document.getElementById('receptionist-section').classList.add('hidden');
    document.getElementById('patient-section').classList.add('hidden');
    
    // Show appropriate section based on role
    if (currentUser.roles.includes('Admin')) {
        document.getElementById('admin-section').classList.remove('hidden');
        loadUsers();
    } else if (currentUser.roles.includes('Doctor')) {
        document.getElementById('doctor-section').classList.remove('hidden');
        loadDoctorPatients();
    } else if (currentUser.roles.includes('Nurse')) {
        document.getElementById('nurse-section').classList.remove('hidden');
    } else if (currentUser.roles.includes('Receptionist')) {
        document.getElementById('receptionist-section').classList.remove('hidden');
        loadDoctorsDropdown();
        loadPatientsList();
    } else if (currentUser.roles.includes('Patient')) {
        document.getElementById('patient-section').classList.remove('hidden');
        loadMyRecords();
        loadMyConsent();
        loadNotifications();
    }
}

// Tab functionality
document.querySelectorAll('.tab-btn').forEach(btn => {
    btn.addEventListener('click', () => {
        const tabId = btn.dataset.tab;
        const parent = btn.closest('.card');
        
        parent.querySelectorAll('.tab-btn').forEach(b => b.classList.remove('active'));
        parent.querySelectorAll('.tab-content').forEach(c => c.classList.add('hidden'));
        
        btn.classList.add('active');
        parent.querySelector('#' + tabId).classList.remove('hidden');
        parent.querySelector('#' + tabId).classList.add('active');
    });
});

// Admin functions
async function loadUsers() {
    try {
        const users = await apiCall('/api/admin/users');
        const html = `<table>
            <tr><th>Email</th><th>Roles</th><th>Patient ID</th></tr>
            ${users.map(u => `<tr>
                <td>${u.email}</td>
                <td>${u.roles.join(', ')}</td>
                <td>${u.patientId || '-'}${u.patientId ? `<button class="btn copy-btn" onclick="copyToClipboard('${u.patientId}')">Copy</button>` : ''}</td>
            </tr>`).join('')}
        </table>`;
        document.getElementById('users-list').innerHTML = html;
    } catch (err) {
        showMessage(err.message, 'error');
    }
}

document.getElementById('create-user-form').addEventListener('submit', async (e) => {
    e.preventDefault();
    const email = document.getElementById('new-user-email').value;
    const password = document.getElementById('new-user-password').value;
    const role = document.getElementById('new-user-role').value;
    const patientId = document.getElementById('new-user-patient-id').value || null;
    
    try {
        await apiCall('/api/auth/register', 'POST', { email, password, role, patientId });
        showMessage('User created successfully!');
        loadUsers();
        e.target.reset();
    } catch (err) {
        showMessage(err.message, 'error');
    }
});

document.getElementById('create-patient-form').addEventListener('submit', async (e) => {
    e.preventDefault();
    const mrn = document.getElementById('patient-mrn').value;
    const fullName = document.getElementById('patient-name').value;
    const dateOfBirth = document.getElementById('patient-dob').value;
    const gender = document.getElementById('patient-gender').value;
    
    try {
        const patient = await apiCall('/api/patients', 'POST', { mrn, fullName, dateOfBirth, gender });
        showMessage(`Patient created! ID: ${patient.id}`);
        document.getElementById('patients-list').innerHTML = `
            <div class="success">
                <strong>Patient Created:</strong><br>
                ID: ${patient.id} <button class="btn copy-btn" onclick="copyToClipboard('${patient.id}')">Copy ID</button><br>
                MRN: ${patient.mrn}<br>
                Name: ${patient.fullName}
            </div>
        `;
        e.target.reset();
    } catch (err) {
        showMessage(err.message, 'error');
    }
});

document.getElementById('refresh-audit').addEventListener('click', async () => {
    try {
        const logs = await apiCall('/api/admin/audit-logs?take=50');
        const html = `<table>
            <tr><th>Time</th><th>Actor</th><th>Action</th><th>Resource</th><th>Patient</th></tr>
            ${logs.map(l => `<tr>
                <td>${new Date(l.timestampUtc).toLocaleString()}</td>
                <td>${l.actorEmail}</td>
                <td>${l.action}</td>
                <td>${l.resource}</td>
                <td>${l.patientId || '-'}</td>
            </tr>`).join('')}
        </table>`;
        document.getElementById('audit-list').innerHTML = html;
    } catch (err) {
        showMessage(err.message, 'error');
    }
});

// Doctor functions
document.getElementById('doctor-view-records').addEventListener('click', async () => {
    const patientId = document.getElementById('doctor-patient-id').value;
    if (!patientId) return showMessage('Enter Patient ID', 'error');
    
    try {
        const records = await apiCall(`/api/records/${patientId}`);
        displayRecords(records, 'doctor-records');
    } catch (err) {
        showMessage(err.message, 'error');
        document.getElementById('doctor-records').innerHTML = `<div class="error">${err.message}</div>`;
    }
});

document.getElementById('add-record-form').addEventListener('submit', async (e) => {
    e.preventDefault();
    const patientId = document.getElementById('doctor-patient-id').value;
    if (!patientId) return showMessage('Enter Patient ID first', 'error');
    
    const diagnosis = document.getElementById('record-diagnosis').value;
    const prescriptions = document.getElementById('record-prescriptions').value;
    const clinicalNotes = document.getElementById('record-notes').value;
    
    try {
        await apiCall(`/api/records/${patientId}`, 'POST', { diagnosis, prescriptions, clinicalNotes });
        showMessage('Record added successfully!');
        document.getElementById('doctor-view-records').click();
        e.target.reset();
    } catch (err) {
        showMessage(err.message, 'error');
    }
});

// Nurse functions
document.getElementById('nurse-view-records').addEventListener('click', async () => {
    const patientId = document.getElementById('nurse-patient-id').value;
    if (!patientId) return showMessage('Enter Patient ID', 'error');
    
    try {
        const records = await apiCall(`/api/records/${patientId}`);
        displayRecords(records, 'nurse-records');
    } catch (err) {
        showMessage(err.message, 'error');
        document.getElementById('nurse-records').innerHTML = `<div class="error">${err.message}</div>`;
    }
});

// Patient functions
async function loadMyRecords() {
    try {
        const records = await apiCall('/api/portal/records');
        displayRecords(records, 'patient-records', true);
    } catch (err) {
        document.getElementById('patient-records').innerHTML = `<div class="error">${err.message}</div>`;
    }
}

document.getElementById('refresh-my-records').addEventListener('click', loadMyRecords);

async function loadMyConsent() {
    try {
        const consent = await apiCall('/api/consents/me');
        document.getElementById('allow-doctors').checked = consent.allowDoctors;
        document.getElementById('allow-nurses').checked = consent.allowNurses;
        document.getElementById('consent-status').innerHTML = `
            <div class="success">Last updated: ${new Date(consent.updatedAtUtc).toLocaleString()}</div>
        `;
    } catch (err) {
        document.getElementById('consent-status').innerHTML = `<div class="error">${err.message}</div>`;
    }
}

document.getElementById('consent-form').addEventListener('submit', async (e) => {
    e.preventDefault();
    const allowDoctors = document.getElementById('allow-doctors').checked;
    const allowNurses = document.getElementById('allow-nurses').checked;
    
    try {
        await apiCall('/api/consents/me', 'PUT', { allowDoctors, allowNurses });
        showMessage('Consent updated!');
        loadMyConsent();
        loadNotifications();
    } catch (err) {
        showMessage(err.message, 'error');
    }
});

// Notification functions
async function loadNotifications() {
    try {
        const notifications = await apiCall('/api/portal/notifications');
        const unreadCount = notifications.filter(n => !n.isRead).length;
        
        const badge = document.getElementById('notif-badge');
        if (unreadCount > 0) {
            badge.textContent = unreadCount;
            badge.classList.remove('hidden');
        } else {
            badge.classList.add('hidden');
        }
        
        if (!notifications.length) {
            document.getElementById('notifications-list').innerHTML = '<p>No notifications yet.</p>';
            return;
        }
        
        const html = notifications.map(n => `
            <div class="notification-item ${n.isRead ? '' : 'unread'}" data-id="${n.id}">
                <h4>${n.title}</h4>
                <p>${n.message}</p>
                <div class="meta">${new Date(n.createdAtUtc).toLocaleString()} ${n.isRead ? '' : '<button class="btn btn-secondary" style="padding:2px 8px;font-size:11px" onclick="markNotifRead(\'' + n.id + '\')">Mark Read</button>'}</div>
            </div>
        `).join('');
        
        document.getElementById('notifications-list').innerHTML = html;
    } catch (err) {
        document.getElementById('notifications-list').innerHTML = `<div class="error">${err.message}</div>`;
    }
}

async function markNotifRead(id) {
    try {
        await apiCall(`/api/portal/notifications/${id}/read`, 'POST');
        loadNotifications();
    } catch (err) {
        showMessage(err.message, 'error');
    }
}

document.getElementById('refresh-notifications').addEventListener('click', loadNotifications);

document.getElementById('mark-all-read').addEventListener('click', async () => {
    try {
        await apiCall('/api/portal/notifications/read-all', 'POST');
        loadNotifications();
        showMessage('All notifications marked as read');
    } catch (err) {
        showMessage(err.message, 'error');
    }
});

// Receptionist functions
async function loadDoctorsDropdown() {
    try {
        const doctors = await apiCall('/api/receptionist/doctors');
        const select = document.getElementById('reg-doctor');
        select.innerHTML = '<option value="">Select Doctor...</option>' + 
            doctors.map(d => `<option value="${d.id}">${d.email}</option>`).join('');
    } catch (err) {
        console.error('Failed to load doctors:', err);
    }
}

async function loadPatientsList() {
    try {
        const patients = await apiCall('/api/receptionist/patients');
        if (!patients.length) {
            document.getElementById('patients-table').innerHTML = '<p>No patients registered yet.</p>';
            return;
        }
        const html = `<table>
            <tr><th>MRN</th><th>Name</th><th>DOB</th><th>Assigned Doctor</th><th>Patient ID</th></tr>
            ${patients.map(p => `<tr>
                <td>${p.mrn}</td>
                <td>${p.fullName}</td>
                <td>${p.dateOfBirth}</td>
                <td>${p.assignedDoctorEmail || 'Unassigned'}</td>
                <td><button class="btn copy-btn" onclick="copyToClipboard('${p.id}')">${p.id.substring(0,8)}... Copy</button></td>
            </tr>`).join('')}
        </table>`;
        document.getElementById('patients-table').innerHTML = html;
    } catch (err) {
        document.getElementById('patients-table').innerHTML = `<div class="error">${err.message}</div>`;
    }
}

document.getElementById('register-patient-form').addEventListener('submit', async (e) => {
    e.preventDefault();
    const mrn = document.getElementById('reg-mrn').value;
    const fullName = document.getElementById('reg-fullname').value;
    const dateOfBirth = document.getElementById('reg-dob').value;
    const gender = document.getElementById('reg-gender').value;
    const assignedDoctorId = document.getElementById('reg-doctor').value || null;
    const contactPhone = document.getElementById('reg-phone').value || null;
    const contactEmail = document.getElementById('reg-email').value || null;
    
    try {
        const result = await apiCall('/api/receptionist/register-patient', 'POST', {
            mrn, fullName, dateOfBirth, gender, assignedDoctorId, contactPhone, contactEmail
        });
        document.getElementById('register-result').innerHTML = `
            <div class="success">
                <strong>Patient Registered!</strong><br>
                ID: ${result.id} <button class="btn copy-btn" onclick="copyToClipboard('${result.id}')">Copy ID</button><br>
                Name: ${result.fullName}<br>
                Doctor: ${result.assignedDoctorEmail || 'Unassigned'}
            </div>`;
        e.target.reset();
        loadPatientsList();
    } catch (err) {
        document.getElementById('register-result').innerHTML = `<div class="error">${err.message}</div>`;
    }
});

document.getElementById('refresh-patients-list').addEventListener('click', loadPatientsList);

// Doctor: view previously attended patients
async function loadDoctorPatients() {
    const container = document.getElementById('doctor-my-patients');
    try {
        const patients = await apiCall('/api/records/my-patients');
        if (patients.length === 0) {
            container.innerHTML = '<p><em>No patients attended yet. Search for a patient below to add records.</em></p>';
            return;
        }
        container.innerHTML = `<table>
            <tr><th>MRN</th><th>Name</th><th>DOB</th><th>Gender</th><th>Records</th><th>Last Visit</th><th>Action</th></tr>
            ${patients.map(p => `<tr>
                <td>${p.mrn || '-'}</td>
                <td>${p.fullName}</td>
                <td>${new Date(p.dateOfBirth).toLocaleDateString()}</td>
                <td>${p.gender}</td>
                <td>${p.recordCount}</td>
                <td>${new Date(p.lastVisit).toLocaleDateString()}</td>
                <td><button class="btn btn-primary" onclick="viewPatientRecords('${p.id}')">View</button></td>
            </tr>`).join('')}
        </table>`;
    } catch (err) {
        container.innerHTML = '<p class="error">Failed to load patients</p>';
    }
}

function viewPatientRecords(patientId) {
    document.getElementById('doctor-patient-id').value = patientId;
    document.getElementById('doctor-view-records').click();
}

// Patient download functions
document.getElementById('download-all-records').addEventListener('click', async () => {
    try {
        const response = await fetch('/api/portal/records/download-all', {
            headers: { 'Authorization': `Bearer ${token}` }
        });
        if (!response.ok) throw new Error('Download failed');
        const html = await response.text();
        const blob = new Blob([html], { type: 'text/html' });
        const url = URL.createObjectURL(blob);
        window.open(url, '_blank');
    } catch (err) {
        showMessage('Failed to download records: ' + err.message, 'error');
    }
});

// Helper functions
async function downloadPrescription(recordId) {
    try {
        const response = await fetch(`/api/portal/prescription/${recordId}/download`, {
            headers: { 'Authorization': `Bearer ${token}` }
        });
        if (!response.ok) throw new Error('Download failed');
        const html = await response.text();
        const blob = new Blob([html], { type: 'text/html' });
        const url = URL.createObjectURL(blob);
        window.open(url, '_blank');
    } catch (err) {
        showMessage('Failed to download: ' + err.message, 'error');
    }
}

function displayRecords(records, containerId, showDownload = false) {
    if (!records.length) {
        document.getElementById(containerId).innerHTML = '<p>No records found.</p>';
        return;
    }
    
    const html = records.map(r => `
        <div class="record-card">
            <h4>Record ${r.id.substring(0, 8)}...</h4>
            <p><strong>Diagnosis:</strong> ${r.diagnosis}</p>
            <p><strong>Prescriptions:</strong> ${r.prescriptions}</p>
            <p><strong>Clinical Notes:</strong> ${r.clinicalNotes}</p>
            <div class="meta">Created: ${new Date(r.createdAtUtc).toLocaleString()}</div>
            ${showDownload ? `<button class="btn btn-secondary" onclick="downloadPrescription('${r.id}')">📥 Download Prescription</button>` : ''}
        </div>
    `).join('');
    
    document.getElementById(containerId).innerHTML = html;
}

function copyToClipboard(text) {
    navigator.clipboard.writeText(text);
    showMessage('Copied to clipboard!');
}

// Check for existing session on load
window.addEventListener('load', () => {
    const savedToken = localStorage.getItem('token');
    const savedUser = localStorage.getItem('user');
    
    if (savedToken && savedUser) {
        token = savedToken;
        currentUser = JSON.parse(savedUser);
        showDashboard();
    }
});
