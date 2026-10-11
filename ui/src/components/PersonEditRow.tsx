import { useId, useRef, useState, type KeyboardEvent, type SubmitEvent } from "react";
import type { Person } from "../api/types";
import styles from "./PersonEditRow.module.css";

interface PersonEditRowProps {
    person: Person
    /** Called with the edited person. If it rejects the row stays open, so the user can retry. */
    onSave: (person: Person) => Promise<void>
    onCancel: () => void
}

// The edit mode of one table row: every column but Id turns into an input, and the last cell
// gets Save and Cancel. Render it in place of the normal row, inside a <tbody>.
export function PersonEditRow({ person, onSave, onCancel }: PersonEditRowProps) {
    const formId = useId()
    const formRef = useRef<HTMLFormElement>(null)
    const [isSaving, setIsSaving] = useState(false)

    async function handleSubmit(e: SubmitEvent<HTMLFormElement>) {
        e.preventDefault()
        // The inputs are uncontrolled: read them from the form (before any await).
        const data = new FormData(e.currentTarget)
        const managerId = String(data.get('managerId'))
        const edited: Person = {
            ...person,
            name: String(data.get('name')).trim(),
            dateOfBirth: String(data.get('dateOfBirth')),
            managerId: managerId === '' ? null : Number(managerId),
            salary: Number(data.get('salary')),
        }

        if (JSON.stringify(edited) === JSON.stringify(person)) {
            onCancel() // nothing changed: no request
            return
        }

        setIsSaving(true)
        try {
            await onSave(edited)
        } catch {
            // The caller reports the failure; stay in edit mode so nothing typed is lost.
            setIsSaving(false)
        }
    }

    function handleKeyDown(e: KeyboardEvent<HTMLTableRowElement>) {
        if (isSaving) return
        if (e.key === 'Escape') {
            e.preventDefault()
            onCancel()
        } else if (e.key === 'Enter' && e.target instanceof HTMLInputElement) {
            // Submit explicitly: the Save button is outside the <form>, so do not rely on implicit submission.
            e.preventDefault()
            formRef.current?.requestSubmit()
        }
    }

    return (
        <tr className="selected" onKeyDown={handleKeyDown}>
            <td>
                {person.id}
                {/* A <form> cannot wrap a <tr>, so the inputs below point at this one with form={formId}. */}
                <form id={formId} ref={formRef} onSubmit={handleSubmit} />
            </td>
            <td>
                <input
                    className={styles.input}
                    form={formId}
                    name='name'
                    type='text'
                    aria-label='Name'
                    required
                    autoFocus
                    readOnly={isSaving}
                    defaultValue={person.name}
                />
            </td>
            <td>
                <input
                    className={styles.input}
                    form={formId}
                    name='dateOfBirth'
                    type='date'
                    aria-label='Date of birth'
                    required
                    readOnly={isSaving}
                    defaultValue={person.dateOfBirth}
                />
            </td>
            <td>
                <input
                    className={styles.input}
                    form={formId}
                    name='managerId'
                    type='number'
                    aria-label='Manager Id'
                    readOnly={isSaving}
                    defaultValue={person.managerId ?? ''}
                />
            </td>
            <td>
                <input
                    className={styles.input}
                    form={formId}
                    name='salary'
                    type='number'
                    aria-label='Salary'
                    required
                    min={0}
                    readOnly={isSaving}
                    defaultValue={person.salary}
                />
            </td>
            <td>
                <div className={styles.actions}>
                    <button type='submit' form={formId} disabled={isSaving}>Save</button>
                    <button type='button' className='button-secondary' disabled={isSaving} onClick={onCancel}>Cancel</button>
                </div>
            </td>
        </tr>
    );
}
