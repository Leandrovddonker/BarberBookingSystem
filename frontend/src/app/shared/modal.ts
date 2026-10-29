import { DOCUMENT } from '@angular/common';
import {
  AfterViewInit,
  Component,
  ElementRef,
  OnDestroy,
  inject,
  input,
  output,
  viewChild,
} from '@angular/core';

let nextDialogId = 0;

@Component({
  selector: 'app-modal',
  templateUrl: './modal.html',
  styleUrl: './modal.css',
})
export class Modal implements AfterViewInit, OnDestroy {
  private readonly document = inject(DOCUMENT);
  private readonly dialog =
    viewChild.required<ElementRef<HTMLDialogElement>>('dialog');
  private previousFocus: HTMLElement | null = null;
  private previousBodyOverflow = '';

  readonly title = input.required<string>();
  readonly busy = input(false);
  readonly fallbackFocus = input<HTMLElement | null>(null);
  readonly dismissed = output<void>();
  readonly titleId = `modal-title-${++nextDialogId}`;

  ngAfterViewInit(): void {
    this.previousFocus = this.document.activeElement as HTMLElement | null;
    this.previousBodyOverflow = this.document.body.style.overflow;
    this.document.body.style.overflow = 'hidden';
    // Native modal dialogs make the rest of the document inert and contain focus.
    this.dialog().nativeElement.showModal();
  }

  onCancel(event: Event): void {
    // Keep the Angular state and the native dialog in sync. A pending write must
    // finish before dismissing; closing a dialog cannot undo a submitted request.
    event.preventDefault();
    if (!this.busy()) this.dismissed.emit();
  }

  ngOnDestroy(): void {
    this.dialog().nativeElement.close();
    this.document.body.style.overflow = this.previousBodyOverflow;
    const previousFocus = this.previousFocus;
    const fallbackFocus = this.fallbackFocus();
    // Wait for a successful deletion's list update before restoring focus.
    queueMicrotask(() => {
      const target = previousFocus?.isConnected ? previousFocus : fallbackFocus;
      if (target?.isConnected) target.focus({ preventScroll: true });
    });
  }
}
