import '@testing-library/jest-dom/vitest'

// jsdom does not implement the HTMLDialogElement methods, so mimic them by toggling `open`.
if (typeof HTMLDialogElement.prototype.showModal !== 'function') {
  HTMLDialogElement.prototype.show = function () {
    this.setAttribute('open', '')
  }
  HTMLDialogElement.prototype.showModal = function () {
    this.setAttribute('open', '')
  }
  HTMLDialogElement.prototype.close = function () {
    this.removeAttribute('open')
  }
}
