// Global variables
let currentRequestId = null;
let currentRequestType = null;

/**
 * Open review modal for approving request
 */
function openReviewModal(requestId, requestType, userName, type, date1, date2, extra, reason, taskDesc) {
    currentRequestId = requestId;
    currentRequestType = requestType;

    let detailsHTML = `
        <div class="tm-modal-review-details">
            <div class="tm-review-user-section">
                <h6><i class="fas fa-user me-2"></i>Thông tin nhân viên</h6>
                <p class="mb-0"><strong>${userName}</strong></p>
            </div>
            <div class="tm-review-request-section">
                <h6><i class="fas fa-file-alt me-2"></i>Chi tiết đề xuất</h6>
    `;

    if (requestType === 'Leave') {
        detailsHTML += `
            <p><strong>Loại nghỉ phép:</strong> ${type}</p>
            <p><strong>Từ ngày:</strong> ${date1}</p>
            <p><strong>Đến ngày:</strong> ${date2}</p>
            <p><strong>Tổng số ngày:</strong> ${extra} ngày</p>
        `;
    } else if (requestType === 'Late') {
        detailsHTML += `
            <p><strong>Loại:</strong> ${type}</p>
            <p><strong>Ngày đi trễ:</strong> ${date1}</p>
            <p><strong>Dự kiến đến lúc:</strong> ${date2}</p>
        `;
    } else if (requestType === 'Overtime') {
        detailsHTML += `
            <p><strong>Ngày làm việc:</strong> ${date1}</p>
            <p><strong>Số giờ tăng ca:</strong> ${date2} giờ</p>
            <p><strong>Thời gian check-out:</strong> ${extra}</p>
            ${taskDesc ? `<p><strong>Công việc thực hiện:</strong> ${taskDesc}</p>` : ''}
        `;
    }

    if (reason) {
        detailsHTML += `<p><strong>Lý do:</strong> ${reason}</p>`;
    }

    detailsHTML += `</div></div>`;

    document.getElementById('reviewDetails').innerHTML = detailsHTML;
    document.getElementById('reviewNote').value = '';

    const modal = new bootstrap.Modal(document.getElementById('reviewModal'));
    modal.show();
}

/**
 * Open reject modal with reason input
 */
function openRejectModal(requestId, requestType) {
    currentRequestId = requestId;
    currentRequestType = requestType;

    let typeText = '';
    if (requestType === 'Leave') {
        typeText = 'nghỉ phép';
    } else if (requestType === 'Late') {
        typeText = 'đi trễ';
    } else if (requestType === 'Overtime') {
        typeText = 'tăng ca';
    }

    Swal.fire({
        title: `Từ chối đề xuất ${typeText}`,
        html: `
            <div class="text-start">
                <label class="form-label fw-bold">
                    Lý do từ chối <span class="text-danger">*</span>
                </label>
                <textarea id="rejectReason" class="form-control" rows="4" 
                          placeholder="Nhập lý do từ chối đề xuất này..."></textarea>
                <small class="text-muted d-block mt-2">
                    <i class="fas fa-info-circle me-1"></i>
                    Lý do từ chối sẽ được gửi đến nhân viên
                </small>
            </div>
        `,
        icon: 'warning',
        showCancelButton: true,
        confirmButtonText: '<i class="fas fa-times me-1"></i> Từ chối',
        cancelButtonText: '<i class="fas fa-undo me-1"></i> Hủy',
        confirmButtonColor: '#E74C3C',
        cancelButtonColor: '#6c757d',
        customClass: {
            confirmButton: 'btn btn-danger',
            cancelButton: 'btn btn-secondary'
        },
        preConfirm: () => {
            const reason = document.getElementById('rejectReason').value.trim();
            if (!reason) {
                Swal.showValidationMessage('Vui lòng nhập lý do từ chối');
                return false;
            }
            if (reason.length < 10) {
                Swal.showValidationMessage('Lý do phải có ít nhất 10 ký tự');
                return false;
            }
            return reason;
        }
    }).then(async (result) => {
        if (result.isConfirmed) {
            await submitReview('Reject', result.value);
        }
    });
}

/**
 * Approve request directly from review modal
 */
async function reviewRequest(action) {
    const note = document.getElementById('reviewNote').value.trim();

    if (action === 'Approve') {
        await submitReview('Approve', note);
    } else if (action === 'Reject') {
        // Close current modal first
        const modalEl = document.getElementById('reviewModal');
        if (modalEl) {
            const modalInstance = bootstrap.Modal.getInstance(modalEl);
            if (modalInstance) {
                modalInstance.hide();
            }
        }
        // Open reject modal with reason requirement
        setTimeout(() => {
            openRejectModal(currentRequestId, currentRequestType);
        }, 300);
    }
}

/**
 * Submit review to server
 */
async function submitReview(action, note) {
    Swal.fire({
        title: 'Đang xử lý...',
        html: '<div class="spinner-border text-primary" role="status"></div>',
        allowOutsideClick: false,
        showConfirmButton: false,
        didOpen: () => {
            Swal.showLoading();
        }
    });

    try {
        const response = await fetch('/Admin/ReviewRequest', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value || ''
            },
            body: JSON.stringify({
                RequestId: currentRequestId,
                RequestType: currentRequestType,
                Action: action,
                Note: note || ''
            })
        });

        const data = await response.json();

        if (data.success) {
            Swal.fire({
                icon: 'success',
                title: 'Thành công!',
                text: data.message,
                confirmButtonText: '<i class="fas fa-check me-1"></i> OK',
                confirmButtonColor: '#27AE60',
                timer: 2000
            }).then(() => {
                // Close modal if exists
                const modalEl = document.getElementById('reviewModal');
                if (modalEl) {
                    const modalInstance = bootstrap.Modal.getInstance(modalEl);
                    if (modalInstance) {
                        modalInstance.hide();
                    }
                }
                // Reload page to show updated data
                location.reload();
            });
        } else {
            Swal.fire({
                icon: 'error',
                title: 'Lỗi!',
                text: data.message,
                confirmButtonText: '<i class="fas fa-times me-1"></i> Đóng',
                confirmButtonColor: '#E74C3C'
            });
        }
    } catch (error) {
        console.error('Error:', error);
        Swal.fire({
            icon: 'error',
            title: 'Lỗi kết nối!',
            text: 'Có lỗi xảy ra khi kết nối đến server: ' + error.message,
            confirmButtonText: '<i class="fas fa-times me-1"></i> Đóng',
            confirmButtonColor: '#E74C3C'
        });
    }
}

/**
 * View request detail (can be implemented later)
 */
function viewRequestDetail(requestId, requestType) {
    Swal.fire({
        title: 'Chi tiết đề xuất',
        html: '<p>Chức năng xem chi tiết đang được phát triển...</p>',
        icon: 'info',
        confirmButtonText: '<i class="fas fa-check me-1"></i> OK',
        confirmButtonColor: '#3498DB'
    });

    console.log('View detail:', { requestId, requestType });
}

/**
 * Refresh page data
 */
function refreshData() {
    Swal.fire({
        title: 'Đang tải lại...',
        html: '<div class="spinner-border text-primary" role="status"></div>',
        allowOutsideClick: false,
        showConfirmButton: false,
        timer: 500,
        didOpen: () => {
            Swal.showLoading();
        }
    }).then(() => {
        location.reload();
    });
}

/**
 * Initialize tooltips
 */
document.addEventListener('DOMContentLoaded', function () {
    // Initialize Bootstrap tooltips if needed
    const tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    tooltipTriggerList.map(function (tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl);
    });

    // Add smooth scroll behavior
    document.querySelectorAll('a[href^="#"]').forEach(anchor => {
        anchor.addEventListener('click', function (e) {
            e.preventDefault();
            const target = document.querySelector(this.getAttribute('href'));
            if (target) {
                target.scrollIntoView({
                    behavior: 'smooth',
                    block: 'start'
                });
            }
        });
    });
});

/**
 * Filter requests by type
 */
function filterRequests(type) {
    const url = new URL(window.location.href);
    if (type) {
        url.searchParams.set('type', type);
    } else {
        url.searchParams.delete('type');
    }
    window.location.href = url.toString();
}

/**
 * Export to Excel (can be implemented later)
 */
function exportToExcel() {
    Swal.fire({
        icon: 'info',
        title: 'Đang phát triển',
        text: 'Chức năng xuất Excel đang được phát triển',
        confirmButtonText: 'OK',
        confirmButtonColor: '#3498DB'
    });
}