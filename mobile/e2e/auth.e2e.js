describe('Authentication', () => {
  beforeAll(async () => {
    await device.launchApp()
  })

  beforeEach(async () => {
    await device.reloadReactNative()
  })

  it('shows login screen on first launch', async () => {
    await expect(element(by.text('Add Account'))).toBeVisible()
  })

  it('can navigate to add account screen', async () => {
    await element(by.text('Add Account')).tap()
    await expect(element(by.text('Server URL'))).toBeVisible()
  })

  it('validates server URL input', async () => {
    await element(by.text('Add Account')).tap()
    const serverInput = element(by.id('server-url-input'))
    await serverInput.typeText('invalid-url')
    await element(by.text('Connect')).tap()
    await expect(element(by.text('Invalid URL'))).toBeVisible()
  })

  it('shows OIDC login option when server supports it', async () => {
    await element(by.text('Add Account')).tap()
    const serverInput = element(by.id('server-url-input'))
    await serverInput.typeText('https://valid-server.example.com')
    await element(by.text('Connect')).tap()
    // Wait for discovery
    await waitFor(element(by.text('Sign In')))
      .toBeVisible()
      .withTimeout(5000)
  })
})
