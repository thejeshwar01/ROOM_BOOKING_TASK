// ///DATE HELPERS///
// The API sends and expects UTC (ISO strings); the page shows and takes input in the browser's local time

function pad(number) {
    return String(number).padStart(2, "0");
}

export function formatDate(iso) {
    return new Date(iso).toLocaleDateString(undefined, { year: "numeric", month: "short", day: "numeric" });
}

export function formatTime(iso) {
    return new Date(iso).toLocaleTimeString(undefined, { hour: "2-digit", minute: "2-digit" });
}

// Date -> "YYYY-MM-DD" for <input type="date">
export function toDateInput(date) {
    return date.getFullYear() + "-" + pad(date.getMonth() + 1) + "-" + pad(date.getDate());
}

// Date or ISO string -> "YYYY-MM-DDTHH:mm" for <input type="datetime-local">
export function toDateTimeInput(value) {
    const date = new Date(value);
    return toDateInput(date) + "T" + pad(date.getHours()) + ":" + pad(date.getMinutes());
}

// "YYYY-MM-DDTHH:mm" in local time -> ISO string in UTC
export function fromDateTimeInput(value) {
    return new Date(value).toISOString();
}

// "YYYY-MM-DD" -> start of that local day, as ISO
export function startOfDay(dateValue) {
    return new Date(dateValue + "T00:00").toISOString();
}

// "YYYY-MM-DD" -> start of the following local day, as ISO (so the whole day is included)
export function endOfDay(dateValue) {
    const date = new Date(dateValue + "T00:00");
    date.setDate(date.getDate() + 1);
    return date.toISOString();
}

// The next full hour from now
export function nextFullHour() {
    const date = new Date();
    date.setHours(date.getHours() + 1, 0, 0, 0);
    return date;
}
