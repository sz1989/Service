import { useEffect, useId, useRef, useState, type PointerEvent, type ReactNode } from "react";
import styles from "./DraggableDialog.module.css";

interface DraggableDialogProps {
    title: string
    onClose: () => void
    children: ReactNode
}

// A modal <dialog> with a caption bar that can be dragged. Mount it to show it, unmount it to hide it.
export function DraggableDialog({ title, onClose, children }: DraggableDialogProps) {
    const titleId = useId()
    const dialogRef = useRef<HTMLDialogElement>(null)

    useEffect(() => {
        const dialog = dialogRef.current
        if (!dialog) return

        dialog.showModal()
        return () => dialog.close()
    }, [])

    // The dialog stays centered by CSS; dragging only adds a translate offset on top of that.
    const [offset, setOffset] = useState({ x: 0, y: 0 })
    const dragStart = useRef<{ pointerX: number, pointerY: number, offsetX: number, offsetY: number } | null>(null)

    function handlePointerDown(e: PointerEvent<HTMLDivElement>) {
        // Let the close button receive its click instead of starting a drag.
        if ((e.target as HTMLElement).closest('button')) return
        e.currentTarget.setPointerCapture(e.pointerId)
        dragStart.current = { pointerX: e.clientX, pointerY: e.clientY, offsetX: offset.x, offsetY: offset.y }
    }

    function handlePointerMove(e: PointerEvent<HTMLDivElement>) {
        const start = dragStart.current
        if (!start) return
        setOffset({
            x: start.offsetX + e.clientX - start.pointerX,
            y: start.offsetY + e.clientY - start.pointerY,
        })
    }

    function handlePointerUp() {
        dragStart.current = null
    }

    return (
        <dialog
            ref={dialogRef}
            className={styles.dialog}
            aria-labelledby={titleId}
            style={{ transform: `translate(${offset.x}px, ${offset.y}px)` }}
            // Esc fires "cancel"; route it through onClose so the parent's state stays in sync.
            onCancel={(e) => { e.preventDefault(); onClose() }}
        >
            <div
                className={styles.caption}
                onPointerDown={handlePointerDown}
                onPointerMove={handlePointerMove}
                onPointerUp={handlePointerUp}
                onPointerCancel={handlePointerUp}
            >
                <span id={titleId}>{title}</span>
                <button type='button' aria-label='Close' className={styles.close} onClick={onClose}>×</button>
            </div>
            {children}
        </dialog>
    );
}
