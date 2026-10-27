import { useCallback, useEffect, useState } from "react";

// Loads data from the API and loads it again whenever one of the dependencies changes.
// reload() fetches again with the same dependencies (e.g. after saving).
export function useApi(load, dependencies) {
    const [data, setData] = useState(null);
    const [error, setError] = useState(null);
    const [loading, setLoading] = useState(true);
    const [version, setVersion] = useState(0);

    useEffect(() => {
        let ignore = false;
        setLoading(true);

        load()
            .then((result) => {
                if (ignore) return;
                setData(result);
                setError(null);
            })
            .catch((err) => { if (!ignore) setError(err); })
            .finally(() => { if (!ignore) setLoading(false); });

        // A newer request replaces this one, so its late answer is ignored
        return () => { ignore = true; };
    }, [...dependencies, version]);

    const reload = useCallback(() => setVersion((v) => v + 1), []);

    return { data, error, loading, reload };
}
