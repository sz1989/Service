import styles from "./Spinner.module.css";

interface SpinnerProps {
    /** Shown next to the icon and announced to screen readers. */
    label?: string
}

export function Spinner({ label = 'Loading…' }: SpinnerProps) {
    return (
        <span role='status' className={styles.spinner}>
            <svg
                className={styles.icon}
                xmlns='http://www.w3.org/2000/svg'
                viewBox='0 0 24 24'
                fill='none'
                stroke='currentColor'
                strokeWidth='3'
                strokeLinecap='round'
                aria-hidden='true'
            >
                <circle cx='12' cy='12' r='9' opacity='0.25' />
                <path d='M21 12a9 9 0 0 0-9-9' />
            </svg>
            {label}
        </span>
    );
}
