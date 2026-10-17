// Form xác thực dùng JavaScript thuần; không phụ thuộc jQuery/CDN.
(() => {
  document.querySelectorAll('[data-toggle-password]').forEach(button => {
    button.addEventListener('click', () => {
      const input = document.getElementById(button.dataset.togglePassword);
      if (!input) return;
      const show = input.type === 'password';
      input.type = show ? 'text' : 'password';
      button.textContent = show ? 'Ẩn' : 'Hiện';
      button.setAttribute('aria-pressed', String(show));
      button.setAttribute('aria-label', show ? 'Ẩn mật khẩu' : 'Hiện mật khẩu');
    });
  });
  document.querySelectorAll('[data-demo-email]').forEach(button => {
    button.addEventListener('click', () => {
      const email = document.getElementById('Input_Email');
      const password = document.getElementById('Input_Password');
      if (email && password) {
        email.value = button.dataset.demoEmail;
        password.value = button.dataset.demoPassword;
        email.dispatchEvent(new Event('input', { bubbles: true }));
        password.dispatchEvent(new Event('input', { bubbles: true }));
        password.focus();
      }
    });
  });
  const confirm = document.getElementById('Input_ConfirmPassword');
  const password = document.getElementById('Input_Password');
  if (confirm && password) {
    const validate = () => confirm.setCustomValidity(
      confirm.value && confirm.value !== password.value ? 'Mật khẩu nhập lại chưa khớp.' : '');
    confirm.addEventListener('input', validate);
    password.addEventListener('input', validate);
  }
})();
