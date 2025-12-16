import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
    vus: 10,
    duration: '30s',
};

const BASE_URL = 'http://host.docker.internal:5129/api/chat'; // Adjust port if needed

export default function () {
    const payload = JSON.stringify({
        message: 'What is the price of StreamBook?',
        history: []
    });

    const params = {
        headers: {
            'Content-Type': 'application/json',
        },
    };

    const res = http.post(`${BASE_URL}/send`, payload, params);

    check(res, {
        'status is 200': (r) => r.status === 200,
        'response time < 2s': (r) => r.timings.duration < 2000,
    });

    sleep(1);
}
