// ///API CLIENT///
// Requests go to /api, which Vite forwards to the ASP.NET back end (see vite.config.js)

// Error returned by the API: { code, message, rule }
export class ApiError extends Error {
    constructor(code, message, rule) {
        super(message);
        this.code = code;
        this.rule = rule;
    }
}

async function request(path, options = {}) {
    let response;
    try {
        response = await fetch("/api" + path, {
            headers: { "Content-Type": "application/json" },
            ...options
        });
    } catch {
        throw new ApiError("NETWORK_ERROR", "Cannot reach the server. Make sure the API is running.");
    }

    const body = await response.json().catch(() => null);
    if (!response.ok) {
        throw new ApiError(
            body?.code ?? "HTTP_" + response.status,
            body?.message ?? "Request failed with status " + response.status + ".",
            body?.rule
        );
    }
    return body;
}

// Builds "?key=value&..." and skips empty values; arrays become repeated keys
function query(params) {
    const search = new URLSearchParams();
    Object.entries(params).forEach(([key, value]) => {
        if (value === undefined || value === null || value === "") return;
        (Array.isArray(value) ? value : [value]).forEach((item) => search.append(key, item));
    });
    const text = search.toString();
    return text ? "?" + text : "";
}

function send(method, path, data) {
    return request(path, { method, body: data === undefined ? undefined : JSON.stringify(data) });
}

export const api = {
    getRooms: (filters = {}) => request("/rooms" + query(filters)),
    getFloors: () => request("/rooms/floors"),

    getUsers: (search) => request("/users" + query({ search })),

    getReservations: (filters = {}) => request("/reservations" + query(filters)),
    getBookingRules: () => request("/reservations/rules"),
    createReservation: (reservation) => send("POST", "/reservations", reservation),
    updateReservation: (id, reservation) => send("PUT", "/reservations/" + id, reservation),
    cancelReservation: (id) => send("POST", "/reservations/" + id + "/cancel"),
    endReservationEarly: (id) => send("POST", "/reservations/" + id + "/end"),

    getRoomUsage: (filters = {}) => request("/reports/room-usage" + query(filters))
};
