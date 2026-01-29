import { useState } from 'react'
import { Button } from '@/components/ui/button'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from '@/components/ui/dialog'
import { Label } from '@/components/ui/label'
import { Input } from '@/components/ui/input'
import { Download, FileText, Inbox } from 'lucide-react'
import { api } from '@/api/client'

interface ExportDialogProps {
  emailId?: string
  trigger?: React.ReactNode
}

export function ExportDialog({ emailId, trigger }: ExportDialogProps) {
  const [isOpen, setIsOpen] = useState(false)
  const [fromDate, setFromDate] = useState('')
  const [toDate, setToDate] = useState('')
  const [isExporting, setIsExporting] = useState(false)

  const handleExportEml = async () => {
    if (!emailId) return

    setIsExporting(true)
    try {
      const headers = await api.getHeaders()
      const response = await fetch(`${api.baseUrl}/emails/${emailId}/export/eml`, {
        headers,
        credentials: 'include',
      })

      if (!response.ok) {
        throw new Error('Export failed')
      }

      const blob = await response.blob()
      const url = window.URL.createObjectURL(blob)
      const a = document.createElement('a')
      a.href = url
      a.download = response.headers.get('Content-Disposition')?.match(/filename="(.+)"/)?.[1] || 'email.eml'
      document.body.appendChild(a)
      a.click()
      window.URL.revokeObjectURL(url)
      document.body.removeChild(a)

      setIsOpen(false)
    } catch (error) {
      console.error('Export failed:', error)
    } finally {
      setIsExporting(false)
    }
  }

  const handleExportMbox = async () => {
    setIsExporting(true)
    try {
      const params = new URLSearchParams()
      if (fromDate) params.set('fromDate', new Date(fromDate).toISOString())
      if (toDate) params.set('toDate', new Date(toDate).toISOString())

      const headers = await api.getHeaders()
      const response = await fetch(`${api.baseUrl}/emails/export/mbox?${params.toString()}`, {
        headers,
        credentials: 'include',
      })

      if (!response.ok) {
        throw new Error('Export failed')
      }

      const blob = await response.blob()
      const url = window.URL.createObjectURL(blob)
      const a = document.createElement('a')
      a.href = url
      a.download = response.headers.get('Content-Disposition')?.match(/filename="(.+)"/)?.[1] || 'emails.mbox'
      document.body.appendChild(a)
      a.click()
      window.URL.revokeObjectURL(url)
      document.body.removeChild(a)

      setIsOpen(false)
    } catch (error) {
      console.error('Export failed:', error)
    } finally {
      setIsExporting(false)
    }
  }

  return (
    <Dialog open={isOpen} onOpenChange={setIsOpen}>
      <DialogTrigger asChild>
        {trigger || (
          <Button variant="outline" size="sm">
            <Download className="h-4 w-4 mr-2" />
            Export
          </Button>
        )}
      </DialogTrigger>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Export Emails</DialogTitle>
          <DialogDescription>
            Download emails in standard formats for backup or import into other email clients
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-6 py-4">
          {emailId && (
            <div>
              <h3 className="font-medium mb-2 flex items-center gap-2">
                <FileText className="h-4 w-4" />
                Single Email (.EML)
              </h3>
              <p className="text-sm text-muted-foreground mb-3">
                Download this email as a standard .EML file that can be opened in any email client
              </p>
              <Button onClick={handleExportEml} disabled={isExporting} className="w-full">
                <Download className="h-4 w-4 mr-2" />
                Download as .EML
              </Button>
            </div>
          )}

          <div className="border-t pt-4">
            <h3 className="font-medium mb-2 flex items-center gap-2">
              <Inbox className="h-4 w-4" />
              All Emails (.MBOX)
            </h3>
            <p className="text-sm text-muted-foreground mb-3">
              Export all your emails as an .MBOX file for backup or import into Thunderbird, Apple Mail, etc.
            </p>

            <div className="space-y-3 mb-3">
              <div>
                <Label htmlFor="fromDate">From Date (optional)</Label>
                <Input
                  id="fromDate"
                  type="date"
                  value={fromDate}
                  onChange={(e) => setFromDate(e.target.value)}
                />
              </div>

              <div>
                <Label htmlFor="toDate">To Date (optional)</Label>
                <Input
                  id="toDate"
                  type="date"
                  value={toDate}
                  onChange={(e) => setToDate(e.target.value)}
                />
              </div>
            </div>

            <Button onClick={handleExportMbox} disabled={isExporting} className="w-full">
              <Download className="h-4 w-4 mr-2" />
              Download as .MBOX
            </Button>
          </div>
        </div>
      </DialogContent>
    </Dialog>
  )
}
