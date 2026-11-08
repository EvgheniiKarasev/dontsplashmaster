import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import worker from './worker.mjs';

for (const slug of ['privacy-policy', 'terms-of-service']) {
  const expected = await readFile(new URL(`${slug}.html`, import.meta.url), 'utf8');
  for (const suffix of ['', '/', '?source=store']) {
    const response = await worker.fetch(new Request(`https://kaevgames.com/vr/${slug}${suffix}`));
    assert.equal(response.status, 200);
    assert.equal((await response.text()).replaceAll('\r\n','\n').trimEnd(), expected.replaceAll('\r\n','\n').trimEnd());
    assert.equal(response.headers.get('Content-Type'), 'text/html; charset=utf-8');
    assert.equal(response.headers.get('Set-Cookie'), null);
  }
  const head = await worker.fetch(new Request(`https://kaevgames.com/vr/${slug}`, { method: 'HEAD' }));
  assert.equal(head.status, 200);
  assert.equal(await head.text(), '');
  assert.equal((await worker.fetch(new Request(`https://kaevgames.com/vr/${slug}`, { method: 'POST' }))).status, 405);
  const http = await worker.fetch(new Request(`http://kaevgames.com/vr/${slug}`));
  assert.equal(http.status, 308);
  assert.equal(http.headers.get('Location'), `https://kaevgames.com/vr/${slug}`);
  const ids = [...expected.matchAll(/\bid="([^"]+)"/g)].map(m => m[1]);
  for (const [, id] of expected.matchAll(/href="#([^"]+)"/g)) assert.ok(ids.includes(id), `Broken anchor ${id}`);
  assert.ok(!/<script|<form|<iframe/i.test(expected));
}
for (const url of ['https://kaevgames.com/vr/privacy-policy-unknown', 'https://kaevgames.com/privacy-policy', 'https://other.example/vr/privacy-policy']) {
  assert.equal((await worker.fetch(new Request(url))).status, 404);
}
console.log('PASS: both documents, slash/query variants, HTTPS redirects, HEAD, method restrictions, route isolation, anchors and no scripts/forms/cookies.');
