//// DepartmentAttendance.js - Client-side functionality for Department Attendance

//class DepartmentAttendance {
//    constructor() {
//        this.autoRefreshInterval = 300000; // 5 minutes
//        this.init();
//    }

//    init() {
//        this.setupEventListeners();
//        this.startAutoRefresh();
//        this.addKeyboardShortcuts();
//    }

//    setupEventListeners() {
//        // Handle form submission
//        document.getElementById('reportForm')?.addEventListener('submit', (e) => {
//            this.showLoading();
//        });

//        // Handle company dropdown change
//        document.getElementById('companyCode')?.addEventListener('change', (e) => {
//            this.onCompanyChange(e.target.value);
//        });

//        // Handle date change
//        document.getElementById('reportDate')?.addEventListener('change', (e) => {
//            this.onDateChange(e.target.value);
//        });

//        // Handle print button
//        document.getElementById('printReport')?.addEventListener('click', () => {
//            this.printReport();
//        });

//        // Handle export button
//        document.getElementById('exportReport')?.addEventListener('click', () => {
//            this.exportReport();
//        });
//    }

//    toggleDepartment(element) {
//        const content = element.nextElementSibling;
//        const icon = element.querySelector('.toggle-icon');

//        if (content.classList.contains('active')) {
//            content.classList.remove('active');
//            element.classList.remove('active');
//            this.saveCollapseState(element.textContent.trim(), false);
//        } else {
//            content.classList.add('active');
//            element.classList.add('active');
//            this.saveCollapseState(element.textContent.trim(), true);
//        }
//    }

//    expandAllDepartments() {
//        const headers = document.querySelectorAll('.department-header');
//        headers.forEach(header => {
//            const content = header.nextElementSibling;
//            content.classList.add('active');
//            header.classList.add('active');
//        });
//    }

//    collapseAllDepartments() {
//        const headers = document.querySelectorAll('.department-header');
//        headers.forEach(header => {
//            const content = header.nextElementSibling;
//            content.classList.remove('active');
//            header.classList.remove('active');
//        });
//    }

//    saveCollapseState(departmentName, isExpanded) {
//        const state = JSON.parse(localStorage.getItem('departmentCollapseState') || '{}');
//        state[departmentName] = isExpanded;
//        localStorage.setItem('departmentCollapseState', JSON.stringify(state));
//    }

//    restoreCollapseState() {
//        const state = JSON.parse(localStorage.getItem('departmentCollapseState') || '{}');
//        const headers = document.querySelectorAll('.department-header');

//        headers.forEach(header => {
//            const departmentName = header.textContent.trim();
//            const content = header.nextElementSibling;

//            if (state[departmentName]) {
//                content.classList.add('active');
//                header.classList.add('active');
//            }
//        });
//    }

//    showAddDepartmentModal() {
//        $('#addDepartmentModal').modal('show');
//    }

//    async addDepartment() {
//        const departmentName = document.getElementById('departmentName').value;
//        const companyCode = document.getElementById('departmentCompany').value;

//        if (!departmentName.trim()) {
//            this.showAlert('Please enter a department name', 'warning');
//            return;
//        }

//        try {
//            this.showLoading();

//            const response = await fetch('/Home/AddDepartment', {
//                method: 'POST',
//                headers: {
//                    'Content-Type': 'application/json',
//                    'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value
//                },
//                body: JSON.stringify({
//                    departmentName: departmentName,
//                    companyCode: parseInt(companyCode)
//                })
//            });

//            const data = await response.json();

//            if (data.success) {
//                this.showAlert('Department added successfully!', 'success');
//                $('#addDepartmentModal').modal('hide');
//                this.refreshPage();
//            } else {
//                this.showAlert('Error: ' + data.message, 'error');
//            }
//        } catch (error) {
//            console.error('Error:', error);
//            this.showAlert('An error occurred while adding the department', 'error');
//        } finally {
//            this.hideLoading();
//        }
//    }

//    async refreshData() {
//        try {
//            this.showLoading();

//            const companyCode = document.getElementById('companyCode').value;
//            const reportDate = document.getElementById('reportDate').value;

//            const response = await fetch('/Home/GetAttendanceStats', {
//                method: 'GET',
//                headers: {
//                    'Content-Type': 'application/json'
//                }
//            });

//            if (response.ok) {
//                const data = await response.json();
//                this.updateStatsDisplay(data);
//                this.showAlert('Data refreshed successfully', 'success');
//            }
//        } catch (error) {
//            console.error('Error refreshing data:', error);
//            this.showAlert('Error refreshing data', 'error');
//        } finally {
//            this.hideLoading();
//        }
//    }

//    updateStatsDisplay(data) {
//        // Update any real-time stats if needed
//        document.getElementById('lastUpdated')?.textContent = data.lastUpdated;
//    }

//    onCompanyChange(companyCode) {
//        // Auto-submit form when company changes
//        document.getElementById('reportForm')?.submit();
//    }

//    onDateChange(reportDate) {
//        // Auto-submit form when date changes
//        document.getElementById('reportForm')?.submit();
//    }

//    printReport() {
//        // Expand all departments before printing
//        this.expandAllDepartments();

//        setTimeout(() => {
//            window.print();
//        }, 500);
//    }

//    exportReport() {
//        // Export functionality - could be Excel, PDF, etc.
//        const reportData = this.collectReportData();
//        this.downloadAsCSV(reportData);
//    }

//    collectReportData() {
//        const data = [];
//        const departments = document.querySelectorAll('.department-accordion');

//        departments.forEach(dept => {
//            const deptName = dept.querySelector('.department-header').textContent.trim();
//            const tables = dept.querySelectorAll('.now-table');

//            tables.forEach(table => {
//                const rows = table.querySelectorAll('tbody tr:not(.totals-row)');
//                rows.forEach(row => {
//                    const cells = row.querySelectorAll('td');
//                    if (cells.length >= 4) {
//                        data.push({
//                            Department: deptName,
//                            NatureOfWork: cells[0].textContent.trim(),
//                            Present: cells[1].textContent.trim(),
//                            Absent: cells[2].textContent.trim(),
//                            Total: cells[3].textContent.trim()
//                        });
//                    }
//                });
//            });
//        });

//        return data;
//    }

//    downloadAsCSV(data) {
//        const headers = ['Department', 'Nature of Work', 'Present', 'Absent', 'Total'];
//        const csvContent = [
//            headers.join(','),
//            ...data.map(row => Object.values(row).join(','))
//        ].join('\n');

//        const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
//        const link = document.createElement('a');
//        const url = URL.createObjectURL(blob);

//        link.setAttribute('href', url);
//        link.setAttribute('download', `department_attendance_${new Date().toISOString().split('T')[0]}.csv`);
//        link.style.visibility = 'hidden';

//        document.body.appendChild(link);
//        link.click();
//        document.body.removeChild(link);
//    }

//    addKeyboardShortcuts() {
//        document.addEventListener('keydown', (e) => {
//            // Ctrl+P for print
//            if (e.ctrlKey && e.key === 'p') {
//                e.preventDefault();
//                this.printReport();
//            }

//            // Ctrl+E for export
//            if (e.ctrlKey && e.key === 'e') {
//                e.preventDefault();
//                this.exportReport();
//            }

//            // Ctrl+R for refresh
//            if (e.ctrlKey && e.key === 'r') {
//                e.preventDefault();
//                this.refreshData();
//            }

//            // Ctrl+A for expand all
//            if (e.ctrlKey && e.key === 'a') {
//                e.preventDefault();
//                this.expandAllDepartments();
//            }

//            // Ctrl+C for collapse all
//            if (e.ctrlKey && e.key === 'c') {
//                e.preventDefault();
//                this.collapseAllDepartments();
//            }
//        });
//    }

//    startAutoRefresh() {
//        setInterval(() => {
//            this.refreshData();
//        }, this.autoRefreshInterval);
//    }

//    showLoading() {
//        const loadingDiv = document.getElementById('loadingSpinner');
//        if (loadingDiv) {
//            loadingDiv.style.display = 'block';
//        }
//    }

//    hideLoading() {
//        const loadingDiv = document.getElementById('loadingSpinner');
//        if (loadingDiv) {
//            loadingDiv.style.display = 'none';
//        }
//    }

//    showAlert(message, type) {
//        // Create toast notification
//        const alertDiv = document.createElement('div');
//        alertDiv.className = `alert alert-${type === 'error' ? 'danger' : type} alert-dismissible fade show`;
//        alertDiv.innerHTML = `
//            ${message}
//            <button type="button" class="close" data-dismiss="alert">
//                <span>&times;</span>
//            </button>
//        `;

//        const container = document.querySelector('.container-fluid');
//        container.insertBefore(alertDiv, container.firstChild);

//        // Auto-dismiss after 5 seconds
//        setTimeout(() => {
//            alertDiv.remove();
//        }, 5000);
//    }

//    refreshPage() {
//        window.location.reload();
//    }
//}

//// Initialize when DOM is loaded
//document.addEventListener('DOMContentLoaded', () => {
//    window.departmentAttendance = new DepartmentAttendance();

//    // Restore collapse state
//    window.departmentAttendance.restoreCollapseState();
//});

//// Global functions for onclick events
//function toggleDepartment(element) {
//    window.departmentAttendance.toggleDepartment(element);
//}

//function showAddDepartmentModal() {
//    window.departmentAttendance.showAddDepartmentModal();
//}

//function addDepartment() {
//    window.departmentAttendance.addDepartment();
//}