import { useActionState } from "react";
import type { NewPerson } from "../api/types";
import styles from "./AddPersonDialog.module.css";
import { DraggableDialog } from "./ui/DraggableDialog";

// Today as YYYY-MM-DD in the user's local time zone (toISOString() would give the UTC date).
function today(): string {
    return new Date().toLocaleDateString('en-CA')
}

interface PersonAddProps{
    onAdd: (person: NewPerson) => Promise<void>
    onCancel: () => void
}

interface FormState {
    error: string | null
    values: {
        name: string
        dateOfBirth: string
        managerId: string
        salary: string
    }
}

const initialState: FormState = {
    error: null,
    values: { name: '', dateOfBirth: today(), managerId: '', salary: '' },
}

export function AddPersonDialog({ onAdd, onCancel }: PersonAddProps) {
    // _prev is required by useActionState's signature but unused.
    async function addPersonAction(_prev: FormState, formData: FormData) : Promise<FormState> {
        const values = {
            name: String(formData.get('name')),
            dateOfBirth: String(formData.get('dateOfBirth')),
            managerId: String(formData.get('managerId')),
            salary: String(formData.get('salary')),
        }
        const person: NewPerson = {
            name: values.name,
            dateOfBirth: values.dateOfBirth,
            managerId: values.managerId === '' ? null : Number(values.managerId),
            salary: Number(values.salary),
        }

        try {
            await onAdd(person)
            return { error: null, values }
        } catch (err) {
            return { error: err instanceof Error ? err.message : 'Save failed.', values }
        }
    }

    const [state, formAction, isPending] = useActionState(addPersonAction, initialState)

    return (
        <DraggableDialog title='Add Person' onClose={onCancel}>
            <form className={styles.form} action={formAction}>
                <label htmlFor='person-name'>Name</label>
                <input id='person-name' name='name' type='text' required />

                <label htmlFor='person-dob'>Date of birth</label>
                <input id='person-dob' name='dateOfBirth' type='date' required defaultValue={state.values.dateOfBirth} />

                <label htmlFor='person-manager'>Manager id</label>
                <input id='person-manager' name='managerId' type='number' />

                <label htmlFor='person-salary'>Salary</label>
                <input id='person-salary' name='salary' type='number' required min={0} />
                {state.error &&  <p role='alert' className={styles.error}>{state.error}</p>}
                <div className={styles.buttons}>
                    <button type='submit' disabled={isPending}>Ok</button>
                    <button type='button' onClick={onCancel}>Cancel</button>
                </div>
            </form>
        </DraggableDialog>
    );
}
