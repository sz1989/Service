import { useEffect, useRef, useState, type FocusEvent, type KeyboardEvent } from "react";
import styles from "./EditableCell.module.css";

interface EditableCellProps {
    /** The current value, as text. */
    value: string
    /** Accessible name of the editor, e.g. "Name". */
    label: string
    type?: 'text' | 'date' | 'number'
    required?: boolean
    min?: number
    /** Shown instead of an empty value. */
    emptyLabel?: string
    /**
     * Called with the trimmed new text when the user commits a change. If it rejects, the edit is
     * discarded and the old value stays: the caller is responsible for reporting the failure.
     */
    onSave: (next: string) => Promise<void> | void
}

// A table cell (<td>) that turns into an input on double-click, Enter or F2.
// Enter or leaving the field saves, Esc cancels. Render it inside a <tr>.
export function EditableCell({
    value,
    label,
    type = 'text',
    required = false,
    min,
    emptyLabel = '—',
    onSave,
}: EditableCellProps) {
    const [isEditing, setIsEditing] = useState(false)
    const [isSaving, setIsSaving] = useState(false)
    const [draft, setDraft] = useState(value)
    const cellRef = useRef<HTMLTableCellElement>(null)
    const inputRef = useRef<HTMLInputElement>(null)
    // Set once an edit has ended (Enter, Esc or blur), so the other events of that edit do nothing.
    const finished = useRef(false)
    // Put the focus back on the cell after a keyboard edit, but not after clicking elsewhere.
    const restoreFocus = useRef(false)

    useEffect(() => {
        if (!isEditing && restoreFocus.current) {
            restoreFocus.current = false
            cellRef.current?.focus()
        }
    }, [isEditing])

    function startEditing() {
        finished.current = false
        setDraft(value)
        setIsEditing(true)
    }

    function cancel(fromKeyboard: boolean) {
        finished.current = true
        restoreFocus.current = fromKeyboard
        setIsEditing(false)
    }

    async function commit(fromKeyboard: boolean) {
        const input = inputRef.current
        if (finished.current || !input) return

        const next = draft.trim()
        input.setCustomValidity(required && next === '' ? 'Please fill out this field.' : '')
        if (!input.checkValidity()) {
            // Enter keeps the editor open and explains the problem; leaving the field drops the edit.
            if (fromKeyboard) input.reportValidity()
            else cancel(false)
            return
        }

        if (next === value) {
            cancel(fromKeyboard)
            return
        }

        finished.current = true
        restoreFocus.current = fromKeyboard
        setIsSaving(true)
        try {
            await onSave(next)
        } catch {
            // The caller reports the failure; the old value stays.
        } finally {
            setIsSaving(false)
            setIsEditing(false)
        }
    }

    function handleCellKeyDown(e: KeyboardEvent<HTMLTableCellElement>) {
        if (e.key === 'Enter' || e.key === 'F2') {
            e.preventDefault()
            startEditing()
        }
    }

    function handleInputKeyDown(e: KeyboardEvent<HTMLInputElement>) {
        if (e.key === 'Enter') {
            e.preventDefault()
            void commit(true)
        } else if (e.key === 'Escape') {
            e.preventDefault()
            cancel(true)
        }
    }

    function handleFocus(e: FocusEvent<HTMLInputElement>) {
        if (type !== 'date') e.target.select()
    }

    return (
        <td
            ref={cellRef}
            className={styles.cell}
            tabIndex={isEditing ? -1 : 0}
            title={isEditing ? undefined : 'Double-click to edit'}
            onDoubleClick={isEditing ? undefined : startEditing}
            onKeyDown={isEditing ? undefined : handleCellKeyDown}
        >
            {isEditing ? (
                <input
                    ref={inputRef}
                    className={styles.input}
                    type={type}
                    aria-label={label}
                    required={required}
                    min={min}
                    value={draft}
                    readOnly={isSaving}
                    autoFocus
                    onFocus={handleFocus}
                    onChange={(e) => setDraft(e.target.value)}
                    onKeyDown={handleInputKeyDown}
                    onBlur={() => void commit(false)}
                />
            ) : (
                value === '' ? emptyLabel : value
            )}
        </td>
    );
}
