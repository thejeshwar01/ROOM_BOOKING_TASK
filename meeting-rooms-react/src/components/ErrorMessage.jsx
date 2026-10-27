// ///ERROR MESSAGE///
// Shows an API error; booking rule violations also show the rule number (R1-R7)
export default function ErrorMessage({ error, className = "" }) {
    if (!error) return null;

    return (
        <div className={"error-message " + className} role="alert">
            <i className="bi bi-exclamation-triangle"></i>
            <div>
                {error.rule && <span className="error-rule">{error.rule}</span>}
                {error.message}
            </div>
        </div>
    );
}
